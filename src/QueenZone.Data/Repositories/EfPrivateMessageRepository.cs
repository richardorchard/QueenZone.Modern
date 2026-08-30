using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using QueenZone.Data.Entities;

namespace QueenZone.Data;

public sealed class EfPrivateMessageRepository(QueenZoneDbContext dbContext) : IPrivateMessageRepository
{
    private const int MaxUniqueConflictRetries = 3;

    public Task<PrivateInboxPage> GetInboxAsync(
        Guid memberId,
        int page = 1,
        int pageSize = PrivateMessageLimits.InboxPageSize,
        CancellationToken cancellationToken = default) =>
        GetInboxCoreAsync(memberId, page, pageSize, isArchived: false, cancellationToken);

    public Task<PrivateInboxPage> GetArchivedInboxAsync(
        Guid memberId,
        int page = 1,
        int pageSize = PrivateMessageLimits.InboxPageSize,
        CancellationToken cancellationToken = default) =>
        GetInboxCoreAsync(memberId, page, pageSize, isArchived: true, cancellationToken);

    public Task<bool> ArchiveConversationAsync(
        Guid conversationId,
        Guid memberId,
        CancellationToken cancellationToken = default) =>
        SetArchivedAsync(conversationId, memberId, isArchived: true, cancellationToken);

    public Task<bool> UnarchiveConversationAsync(
        Guid conversationId,
        Guid memberId,
        CancellationToken cancellationToken = default) =>
        SetArchivedAsync(conversationId, memberId, isArchived: false, cancellationToken);

    private async Task<bool> SetArchivedAsync(
        Guid conversationId,
        Guid memberId,
        bool isArchived,
        CancellationToken cancellationToken)
    {
        // Tracked query + mutate + save, matching UnarchiveAsync below: ExecuteUpdateAsync bypasses
        // the change tracker and would leave any already-tracked participant instance stale within
        // the same DbContext (e.g. a subsequent reply's own unarchive-on-send within one request).
        var participant = await dbContext.PrivateConversationParticipants
            .SingleOrDefaultAsync(
                p => p.ConversationId == conversationId && p.MemberId == memberId,
                cancellationToken);
        if (participant is null)
        {
            return false;
        }

        participant.IsArchived = isArchived;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<PrivateInboxPage> GetInboxCoreAsync(
        Guid memberId,
        int page,
        int pageSize,
        bool isArchived,
        CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, PrivateMessageLimits.MaxInboxPageSize);

        if (IsSqliteDatabase())
        {
            return await GetInboxSqliteAsync(memberId, page, pageSize, isArchived, cancellationToken);
        }

        var totalCount = await dbContext.PrivateConversationParticipants
            .AsNoTracking()
            .CountAsync(
                p => p.MemberId == memberId && p.IsArchived == isArchived && !p.IsRemoved,
                cancellationToken);
        var totalPages = totalCount <= 0 ? 1 : (totalCount + pageSize - 1) / pageSize;
        page = Math.Min(page, totalPages);

        // Unread count is folded into this projection (a correlated scalar subquery SQL Server
        // executes as an APPLY within the same SELECT) so the page and its unread counts come back
        // in one round trip instead of two.
        var pageRows = await dbContext.PrivateConversationParticipants
            .AsNoTracking()
            .Where(p => p.MemberId == memberId && p.IsArchived == isArchived && !p.IsRemoved)
            .OrderByDescending(p => p.Conversation!.LastMessageSortKey)
            .ThenByDescending(p => p.ConversationId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new InboxPageRowWithUnread(
                p.ConversationId,
                p.Conversation!.LastMessagePreview,
                p.Conversation.LastMessageAt,
                p.Conversation.MemberLowId == memberId
                    ? (p.Conversation.MemberHigh != null ? p.Conversation.MemberHigh.DisplayName : string.Empty)
                    : (p.Conversation.MemberLow != null ? p.Conversation.MemberLow.DisplayName : string.Empty),
                p.Conversation.MemberLowId == memberId
                    ? p.Conversation.MemberHighId
                    : p.Conversation.MemberLowId,
                dbContext.PrivateMessages.Count(m =>
                    m.ConversationId == p.ConversationId
                    && m.SenderMemberId != memberId
                    && (p.LastReadSortKey == null || m.SortKey > p.LastReadSortKey))))
            .ToListAsync(cancellationToken);

        return new PrivateInboxPage(MapInboxWithUnread(pageRows), totalCount, page, pageSize);
    }

    public Task<int> CountUnreadConversationsAsync(
        Guid memberId,
        CancellationToken cancellationToken = default) =>
        IsSqliteDatabase()
            ? CountUnreadConversationsSqliteAsync(memberId, cancellationToken)
            : CountUnreadConversationsSqlAsync(memberId, cancellationToken);

    public async Task<PrivateConversationDetail?> GetConversationAsync(
        Guid conversationId,
        Guid memberId,
        int? page = null,
        int pageSize = PrivateMessageLimits.ConversationPageSize,
        CancellationToken cancellationToken = default)
    {
        pageSize = Math.Clamp(pageSize, 1, PrivateMessageLimits.MaxConversationPageSize);

        // Participant and conversation (with both member navigations) in one round trip, via the
        // participant's Conversation navigation, instead of two separate SingleOrDefaultAsync calls.
        var participant = await dbContext.PrivateConversationParticipants
            .AsNoTracking()
            .Include(p => p.Conversation!.MemberLow)
            .Include(p => p.Conversation!.MemberHigh)
            .SingleOrDefaultAsync(
                p => p.ConversationId == conversationId && p.MemberId == memberId,
                cancellationToken);
        if (participant?.Conversation is null)
        {
            return null;
        }

        var conversation = participant.Conversation;

        var otherId = conversation.MemberLowId == memberId
            ? conversation.MemberHighId
            : conversation.MemberLowId;
        var otherName = conversation.MemberLowId == memberId
            ? conversation.MemberHigh?.DisplayName
            : conversation.MemberLow?.DisplayName;

        var totalCount = await dbContext.PrivateMessages
            .AsNoTracking()
            .CountAsync(m => m.ConversationId == conversationId, cancellationToken);
        var totalPages = totalCount <= 0
            ? 1
            : (totalCount + pageSize - 1) / pageSize;

        // Default and explicit last page use a keyset "latest window" (newest pageSize messages).
        // That avoids count/offset TOCTOU races and always surfaces the tip; when the final
        // offset page would be a short remainder, this window can overlap the previous page.
        var effectivePage = page is null or < 1
            ? totalPages
            : Math.Min(page.Value, totalPages);
        var useLatestWindow = page is null or < 1 || effectivePage >= totalPages;

        List<ConversationMessageRow> messageRows;
        if (useLatestWindow)
        {
            var latestRows = await dbContext.PrivateMessages
                .AsNoTracking()
                .Where(m => m.ConversationId == conversationId)
                .OrderByDescending(m => m.SortKey)
                .Take(pageSize)
                .Select(m => new ConversationMessageRow(
                    m.Id,
                    m.SenderMemberId,
                    m.Sender != null ? m.Sender.DisplayName : string.Empty,
                    m.Body,
                    m.CreatedAt,
                    m.SortKey))
                .ToListAsync(cancellationToken);
            messageRows = latestRows
                .OrderBy(m => m.SortKey)
                .ToList();
            effectivePage = totalPages;
        }
        else
        {
            messageRows = await dbContext.PrivateMessages
                .AsNoTracking()
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.SortKey)
                .Skip((effectivePage - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new ConversationMessageRow(
                    m.Id,
                    m.SenderMemberId,
                    m.Sender != null ? m.Sender.DisplayName : string.Empty,
                    m.Body,
                    m.CreatedAt,
                    m.SortKey))
                .ToListAsync(cancellationToken);
        }

        var reportedIds = await LoadReportedMessageIdsAsync(
            conversationId,
            memberId,
            messageRows.Select(m => m.Id),
            cancellationToken);
        IReadOnlyList<PrivateMessageItem> items = messageRows
            .Select(m => new PrivateMessageItem(
                m.Id,
                m.SenderMemberId,
                string.IsNullOrWhiteSpace(m.SenderName) ? "Unknown member" : m.SenderName,
                m.Body,
                m.CreatedAt,
                m.SenderMemberId == memberId,
                m.SortKey,
                reportedIds.Contains(m.Id)))
            .ToList();

        return new PrivateConversationDetail(
            conversationId,
            otherId,
            string.IsNullOrWhiteSpace(otherName) ? "Unknown member" : otherName,
            items,
            totalCount,
            effectivePage,
            pageSize);
    }

    public Task<bool> IsParticipantAsync(
        Guid conversationId,
        Guid memberId,
        CancellationToken cancellationToken = default) =>
        dbContext.PrivateConversationParticipants
            .AsNoTracking()
            .AnyAsync(p => p.ConversationId == conversationId && p.MemberId == memberId, cancellationToken);

    public async Task MarkConversationReadAsync(
        Guid conversationId,
        Guid memberId,
        long lastReadSortKey,
        DateTimeOffset readAt,
        CancellationToken cancellationToken = default)
    {
        // Conditional update so concurrent opens cannot move the cursor backwards.
        await dbContext.PrivateConversationParticipants
            .Where(p =>
                p.ConversationId == conversationId
                && p.MemberId == memberId
                && (p.LastReadSortKey == null || p.LastReadSortKey < lastReadSortKey))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(p => p.LastReadSortKey, lastReadSortKey)
                    .SetProperty(p => p.LastReadAt, readAt),
                cancellationToken);
    }

    public async Task<PrivateMessageSendResult> SendNewOrExistingAsync(
        Guid senderMemberId,
        Guid recipientMemberId,
        string body,
        DateTimeOffset sentAt,
        CancellationToken cancellationToken = default)
    {
        body = NormalizeBody(body);
        if (body.Length == 0)
        {
            return new PrivateMessageSendResult(false, null, "Message body is required.");
        }

        if (body.Length > PrivateMessageLimits.MaxBodyLength)
        {
            return new PrivateMessageSendResult(
                false,
                null,
                $"Message body must be {PrivateMessageLimits.MaxBodyLength} characters or fewer.");
        }

        if (senderMemberId == recipientMemberId)
        {
            return new PrivateMessageSendResult(false, null, "You cannot message yourself.");
        }

        var recipientExists = await dbContext.MemberAccounts
            .AsNoTracking()
            .AnyAsync(m => m.Id == recipientMemberId, cancellationToken);
        if (!recipientExists)
        {
            return new PrivateMessageSendResult(false, null, "Recipient was not found.");
        }

        var preview = TruncatePreview(body);
        for (var attempt = 1; attempt <= MaxUniqueConflictRetries; attempt++)
        {
            try
            {
                var conversationId = await SendOnceAsync(
                    senderMemberId,
                    recipientMemberId,
                    body,
                    preview,
                    sentAt,
                    cancellationToken);
                return new PrivateMessageSendResult(true, conversationId, null);
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex) && attempt < MaxUniqueConflictRetries)
            {
                dbContext.ChangeTracker.Clear();
            }
        }

        return new PrivateMessageSendResult(false, null, "Unable to start the conversation. Please try again.");
    }

    public async Task<PrivateMessageSendResult> ReplyAsync(
        Guid conversationId,
        Guid senderMemberId,
        string body,
        DateTimeOffset sentAt,
        CancellationToken cancellationToken = default)
    {
        body = NormalizeBody(body);
        if (body.Length == 0)
        {
            return new PrivateMessageSendResult(false, null, "Message body is required.");
        }

        if (body.Length > PrivateMessageLimits.MaxBodyLength)
        {
            return new PrivateMessageSendResult(
                false,
                null,
                $"Message body must be {PrivateMessageLimits.MaxBodyLength} characters or fewer.");
        }

        var isParticipant = await IsParticipantAsync(conversationId, senderMemberId, cancellationToken);
        if (!isParticipant)
        {
            return new PrivateMessageSendResult(
                false,
                null,
                "You are not a participant in this conversation.");
        }

        var preview = TruncatePreview(body);
        // Explicit transactions must run through the configured execution strategy so Azure SQL
        // can retry the entire unit of work rather than rejecting a user-started transaction.
        var strategy = dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var conversation = await LockConversationForWriteAsync(conversationId, cancellationToken);
                if (conversation is null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return new PrivateMessageSendResult(false, null, "Conversation not found.");
                }

                var message = new PrivateMessageEntity
                {
                    Id = Guid.NewGuid(),
                    ConversationId = conversationId,
                    SenderMemberId = senderMemberId,
                    Body = body,
                    CreatedAt = sentAt,
                };
                await PrepareMessageSortKeyAsync(message, cancellationToken);
                dbContext.PrivateMessages.Add(message);

                await ReactivateParticipantAsync(conversationId, conversation.MemberLowId, cancellationToken);
                await ReactivateParticipantAsync(conversationId, conversation.MemberHighId, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);

                await UpdateConversationSummaryForInsertedMessageAsync(
                    conversationId,
                    sentAt,
                    preview,
                    senderMemberId,
                    message.SortKey,
                    cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                return new PrivateMessageSendResult(true, conversationId, null);
            }
            catch
            {
                await transaction.RollbackAsync(CancellationToken.None);
                dbContext.ChangeTracker.Clear();
                throw;
            }
        });
    }

    private async Task<Guid> SendOnceAsync(
        Guid senderMemberId,
        Guid recipientMemberId,
        string body,
        string preview,
        DateTimeOffset sentAt,
        CancellationToken cancellationToken)
    {
        var (low, high) = OrderPair(senderMemberId, recipientMemberId);
        // See ReplyAsync: retry the complete transaction when SQL Server reports a transient fault.
        var strategy = dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var conversation = await LockConversationForWriteAsync(low, high, cancellationToken);

                if (conversation is null)
                {
                    conversation = new PrivateConversationEntity
                    {
                        Id = Guid.NewGuid(),
                        MemberLowId = low,
                        MemberHighId = high,
                        CreatedAt = sentAt,
                        LastMessageAt = sentAt,
                        LastMessageSortKey = 0,
                        LastMessagePreview = preview,
                        LastMessageSenderId = senderMemberId,
                    };
                    dbContext.PrivateConversations.Add(conversation);
                    dbContext.PrivateConversationParticipants.Add(new PrivateConversationParticipantEntity
                    {
                        ConversationId = conversation.Id,
                        MemberId = senderMemberId,
                        LastReadAt = sentAt,
                        LastReadSortKey = null,
                        IsArchived = false,
                        IsRemoved = false,
                    });
                    dbContext.PrivateConversationParticipants.Add(new PrivateConversationParticipantEntity
                    {
                        ConversationId = conversation.Id,
                        MemberId = recipientMemberId,
                        LastReadAt = null,
                        LastReadSortKey = null,
                        IsArchived = false,
                        IsRemoved = false,
                    });

                    var firstMessage = new PrivateMessageEntity
                    {
                        Id = Guid.NewGuid(),
                        ConversationId = conversation.Id,
                        SenderMemberId = senderMemberId,
                        Body = body,
                        CreatedAt = sentAt,
                    };
                    await PrepareMessageSortKeyAsync(firstMessage, cancellationToken);
                    dbContext.PrivateMessages.Add(firstMessage);
                    await dbContext.SaveChangesAsync(cancellationToken);

                    // Sender has seen their own first message; align SortKey cursor with LastReadAt.
                    // Tip SortKey is only known after IDENTITY (SQL Server) or in-process assignment (SQLite).
                    conversation.LastMessageSortKey = firstMessage.SortKey;
                    var senderParticipant = await dbContext.PrivateConversationParticipants
                        .SingleAsync(
                            p => p.ConversationId == conversation.Id && p.MemberId == senderMemberId,
                            cancellationToken);
                    senderParticipant.LastReadSortKey = firstMessage.SortKey;
                    senderParticipant.LastReadAt = sentAt;
                    await dbContext.SaveChangesAsync(cancellationToken);

                    await transaction.CommitAsync(cancellationToken);
                    return conversation.Id;
                }

                var conversationId = conversation.Id;

                await EnsureParticipantAsync(conversationId, senderMemberId, cancellationToken);
                await EnsureParticipantAsync(conversationId, recipientMemberId, cancellationToken);
                await ReactivateParticipantAsync(conversationId, senderMemberId, cancellationToken);
                await ReactivateParticipantAsync(conversationId, recipientMemberId, cancellationToken);

                var message = new PrivateMessageEntity
                {
                    Id = Guid.NewGuid(),
                    ConversationId = conversationId,
                    SenderMemberId = senderMemberId,
                    Body = body,
                    CreatedAt = sentAt,
                };
                await PrepareMessageSortKeyAsync(message, cancellationToken);
                dbContext.PrivateMessages.Add(message);
                await dbContext.SaveChangesAsync(cancellationToken);

                await UpdateConversationSummaryForInsertedMessageAsync(
                    conversationId,
                    sentAt,
                    preview,
                    senderMemberId,
                    message.SortKey,
                    cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                return conversationId;
            }
            catch
            {
                await transaction.RollbackAsync(CancellationToken.None);
                dbContext.ChangeTracker.Clear();
                throw;
            }
        });
    }

    private async Task<PrivateConversationEntity?> LockConversationForWriteAsync(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        // The no-op update acquires a write lock held until commit. Every message for an existing
        // conversation therefore receives its identity SortKey only after the previous writer has
        // committed, making SortKey a safe visible-order/read cursor within that conversation.
        var affected = await dbContext.PrivateConversations
            .Where(c => c.Id == conversationId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(c => c.LastMessageAt, c => c.LastMessageAt),
                cancellationToken);
        if (affected == 0)
        {
            return null;
        }

        return await dbContext.PrivateConversations
            .AsNoTracking()
            .SingleAsync(c => c.Id == conversationId, cancellationToken);
    }

    private async Task<PrivateConversationEntity?> LockConversationForWriteAsync(
        Guid memberLowId,
        Guid memberHighId,
        CancellationToken cancellationToken)
    {
        // Serialize the existing-conversation path before allocating the next message SortKey.
        // A concurrent first-send can still see no row; the unique-pair retry handles that race.
        var affected = await dbContext.PrivateConversations
            .Where(c => c.MemberLowId == memberLowId && c.MemberHighId == memberHighId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(c => c.LastMessageAt, c => c.LastMessageAt),
                cancellationToken);
        if (affected == 0)
        {
            return null;
        }

        return await dbContext.PrivateConversations
            .AsNoTracking()
            .SingleAsync(
                c => c.MemberLowId == memberLowId && c.MemberHighId == memberHighId,
                cancellationToken);
    }

    private async Task PrepareMessageSortKeyAsync(
        PrivateMessageEntity message,
        CancellationToken cancellationToken)
    {
        // SQL Server uses IDENTITY via ValueGeneratedOnAdd. SQLite EnsureCreated does not
        // auto-generate non-PK integers, so assign a monotonic SortKey in-process.
        if (!IsSqliteDatabase())
        {
            return;
        }

        var max = await dbContext.PrivateMessages
            .AsNoTracking()
            .Select(m => (long?)m.SortKey)
            .MaxAsync(cancellationToken) ?? 0L;
        message.SortKey = max + 1;
    }

    private async Task UpdateConversationSummaryForInsertedMessageAsync(
        Guid conversationId,
        DateTimeOffset sentAt,
        string preview,
        Guid senderMemberId,
        long sortKey,
        CancellationToken cancellationToken)
    {
        // Callers hold the conversation write lock, so this insert is the tip by SortKey.
        // Always refresh preview/sender/SortKey tip. Keep LastMessageAt monotonic (max of existing
        // and sentAt) for display; inbox ranking uses LastMessageSortKey instead.
        var conversation = await dbContext.PrivateConversations
            .SingleOrDefaultAsync(c => c.Id == conversationId, cancellationToken);
        if (conversation is null)
        {
            return;
        }

        if (sentAt > conversation.LastMessageAt)
        {
            conversation.LastMessageAt = sentAt;
        }

        conversation.LastMessageSortKey = sortKey;
        conversation.LastMessagePreview = preview;
        conversation.LastMessageSenderId = senderMemberId;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<int> CountUnreadConversationsSqlAsync(
        Guid memberId,
        CancellationToken cancellationToken)
    {
        // Join + Distinct/Count instead of a per-row correlated Any() subquery, matching the
        // batched aggregate join CountUnreadForPageSqlAsync already uses.
        return await (
            from m in dbContext.PrivateMessages.AsNoTracking()
            join p in dbContext.PrivateConversationParticipants.AsNoTracking()
                on m.ConversationId equals p.ConversationId
            where p.MemberId == memberId
                && !p.IsArchived
                && !p.IsRemoved
                && m.SenderMemberId != memberId
                && (p.LastReadSortKey == null || m.SortKey > p.LastReadSortKey)
            select m.ConversationId)
            .Distinct()
            .CountAsync(cancellationToken);
    }

    private async Task<int> CountUnreadConversationsSqliteAsync(
        Guid memberId,
        CancellationToken cancellationToken)
    {
        var pageRows = await dbContext.PrivateConversationParticipants
            .AsNoTracking()
            .Where(p => p.MemberId == memberId && !p.IsArchived && !p.IsRemoved)
            .Select(p => new InboxPageRow(
                p.ConversationId,
                p.LastReadSortKey,
                string.Empty,
                default,
                string.Empty,
                Guid.Empty))
            .ToListAsync(cancellationToken);
        if (pageRows.Count == 0)
        {
            return 0;
        }

        var unread = await CountUnreadForPageSqlAsync(memberId, pageRows, cancellationToken);
        return unread.Count(pair => pair.Value > 0);
    }

    private async Task<PrivateInboxPage> GetInboxSqliteAsync(
        Guid memberId,
        int page,
        int pageSize,
        bool isArchived,
        CancellationToken cancellationToken)
    {
        var rows = await dbContext.PrivateConversationParticipants
            .AsNoTracking()
            .Where(p => p.MemberId == memberId && p.IsArchived == isArchived && !p.IsRemoved)
            .Select(p => new
            {
                p.ConversationId,
                p.LastReadSortKey,
                Preview = p.Conversation!.LastMessagePreview,
                LastMessageAt = p.Conversation.LastMessageAt,
                LastMessageSortKey = p.Conversation.LastMessageSortKey,
                OtherDisplayName = p.Conversation.MemberLowId == memberId
                    ? (p.Conversation.MemberHigh != null ? p.Conversation.MemberHigh.DisplayName : string.Empty)
                    : (p.Conversation.MemberLow != null ? p.Conversation.MemberLow.DisplayName : string.Empty),
                OtherId = p.Conversation.MemberLowId == memberId
                    ? p.Conversation.MemberHighId
                    : p.Conversation.MemberLowId,
            })
            .ToListAsync(cancellationToken);

        var totalCount = rows.Count;
        var totalPages = totalCount <= 0 ? 1 : (totalCount + pageSize - 1) / pageSize;
        page = Math.Min(page, totalPages);

        var pageRows = rows
            .OrderByDescending(r => r.LastMessageSortKey)
            .ThenByDescending(r => r.ConversationId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new InboxPageRow(
                r.ConversationId,
                r.LastReadSortKey,
                r.Preview,
                r.LastMessageAt,
                r.OtherDisplayName,
                r.OtherId))
            .ToList();

        var unreadByConversation = await CountUnreadForPageSqlAsync(memberId, pageRows, cancellationToken);
        return new PrivateInboxPage(MapInbox(pageRows, unreadByConversation), totalCount, page, pageSize);
    }

    private async Task<Dictionary<Guid, int>> CountUnreadForPageSqlAsync(
        Guid memberId,
        IReadOnlyList<InboxPageRow> pageRows,
        CancellationToken cancellationToken)
    {
        if (pageRows.Count == 0)
        {
            return [];
        }

        var conversationIds = pageRows.Select(r => r.ConversationId).ToList();

        // SQL filtered aggregate: only unread (SortKey > cursor) messages, grouped by conversation.
        var aggregated = await (
            from m in dbContext.PrivateMessages.AsNoTracking()
            join p in dbContext.PrivateConversationParticipants.AsNoTracking()
                on m.ConversationId equals p.ConversationId
            where conversationIds.Contains(m.ConversationId)
                && p.MemberId == memberId
                && m.SenderMemberId != memberId
                && (p.LastReadSortKey == null || m.SortKey > p.LastReadSortKey)
            group m by m.ConversationId
            into g
            select new { ConversationId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var result = conversationIds.ToDictionary(id => id, _ => 0);
        foreach (var row in aggregated)
        {
            result[row.ConversationId] = row.Count;
        }

        return result;
    }

    private static IReadOnlyList<PrivateConversationListItem> MapInbox(
        IReadOnlyList<InboxPageRow> pageRows,
        IReadOnlyDictionary<Guid, int> unreadByConversation) =>
        pageRows
            .Select(r =>
            {
                var unread = unreadByConversation.GetValueOrDefault(r.ConversationId);
                return new PrivateConversationListItem(
                    r.ConversationId,
                    r.OtherId,
                    string.IsNullOrWhiteSpace(r.OtherDisplayName) ? "Unknown member" : r.OtherDisplayName,
                    r.Preview,
                    r.LastMessageAt,
                    unread > 0,
                    unread);
            })
            .ToList();

    private static IReadOnlyList<PrivateConversationListItem> MapInboxWithUnread(
        IReadOnlyList<InboxPageRowWithUnread> pageRows) =>
        pageRows
            .Select(r => new PrivateConversationListItem(
                r.ConversationId,
                r.OtherId,
                string.IsNullOrWhiteSpace(r.OtherDisplayName) ? "Unknown member" : r.OtherDisplayName,
                r.Preview,
                r.LastMessageAt,
                r.UnreadCount > 0,
                r.UnreadCount))
            .ToList();

    private async Task EnsureParticipantAsync(
        Guid conversationId,
        Guid memberId,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.PrivateConversationParticipants
            .AnyAsync(p => p.ConversationId == conversationId && p.MemberId == memberId, cancellationToken);
        if (exists)
        {
            return;
        }

        dbContext.PrivateConversationParticipants.Add(new PrivateConversationParticipantEntity
        {
            ConversationId = conversationId,
            MemberId = memberId,
            LastReadAt = null,
            LastReadSortKey = null,
            IsArchived = false,
            IsRemoved = false,
        });
    }

    private async Task ReactivateParticipantAsync(
        Guid conversationId,
        Guid memberId,
        CancellationToken cancellationToken)
    {
        // New activity restores visibility rather than silently dropping the message on an
        // archived or removed conversation.
        var participant = await dbContext.PrivateConversationParticipants
            .SingleOrDefaultAsync(
                p => p.ConversationId == conversationId && p.MemberId == memberId,
                cancellationToken);
        if (participant is not null)
        {
            participant.IsArchived = false;
            participant.IsRemoved = false;
        }
    }

    public async Task<bool> RemoveConversationAsync(
        Guid conversationId,
        Guid memberId,
        CancellationToken cancellationToken = default)
    {
        // Tracked query + mutate + save (not ExecuteUpdateAsync) so this stays consistent with
        // ReactivateParticipantAsync within the same DbContext: bypassing the change tracker would
        // leave an already-tracked participant instance stale for a later reply in the same request.
        var participant = await dbContext.PrivateConversationParticipants
            .SingleOrDefaultAsync(
                p => p.ConversationId == conversationId && p.MemberId == memberId,
                cancellationToken);
        if (participant is null)
        {
            return false;
        }

        participant.IsRemoved = true;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<Guid?> GetOtherParticipantIdAsync(
        Guid conversationId,
        Guid memberId,
        CancellationToken cancellationToken = default)
    {
        var conversation = await dbContext.PrivateConversations
            .AsNoTracking()
            .Where(c => c.Id == conversationId
                && (c.MemberLowId == memberId || c.MemberHighId == memberId))
            .Select(c => new { c.MemberLowId, c.MemberHighId })
            .SingleOrDefaultAsync(cancellationToken);
        if (conversation is null)
        {
            return null;
        }

        return conversation.MemberLowId == memberId
            ? conversation.MemberHighId
            : conversation.MemberLowId;
    }

    public Task<bool> HasConversationBetweenAsync(
        Guid memberA,
        Guid memberB,
        CancellationToken cancellationToken = default)
    {
        var (low, high) = OrderPair(memberA, memberB);
        return dbContext.PrivateConversations
            .AsNoTracking()
            .AnyAsync(c => c.MemberLowId == low && c.MemberHighId == high, cancellationToken);
    }

    public Task<bool> IsBlockedAsync(
        Guid blockerMemberId,
        Guid blockedMemberId,
        CancellationToken cancellationToken = default) =>
        dbContext.MemberMessageBlocks
            .AsNoTracking()
            .AnyAsync(
                b => b.BlockerMemberId == blockerMemberId && b.BlockedMemberId == blockedMemberId,
                cancellationToken);

    public Task<bool> IsMessagingBlockedAsync(
        Guid memberA,
        Guid memberB,
        CancellationToken cancellationToken = default) =>
        dbContext.MemberMessageBlocks
            .AsNoTracking()
            .AnyAsync(
                b => (b.BlockerMemberId == memberA && b.BlockedMemberId == memberB)
                    || (b.BlockerMemberId == memberB && b.BlockedMemberId == memberA),
                cancellationToken);

    public async Task BlockAsync(
        Guid blockerMemberId,
        Guid blockedMemberId,
        DateTimeOffset blockedAt,
        CancellationToken cancellationToken = default)
    {
        var exists = await IsBlockedAsync(blockerMemberId, blockedMemberId, cancellationToken);
        if (exists)
        {
            return;
        }

        dbContext.MemberMessageBlocks.Add(new MemberMessageBlockEntity
        {
            Id = Guid.NewGuid(),
            BlockerMemberId = blockerMemberId,
            BlockedMemberId = blockedMemberId,
            CreatedAt = blockedAt,
        });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            // Concurrent block insert — treat as already blocked.
            dbContext.ChangeTracker.Clear();
        }
    }

    public async Task<bool> UnblockAsync(
        Guid blockerMemberId,
        Guid blockedMemberId,
        CancellationToken cancellationToken = default)
    {
        var deleted = await dbContext.MemberMessageBlocks
            .Where(b => b.BlockerMemberId == blockerMemberId && b.BlockedMemberId == blockedMemberId)
            .ExecuteDeleteAsync(cancellationToken);
        return deleted > 0;
    }

    public Task<int> CountMessagesBySenderSinceAsync(
        Guid senderMemberId,
        DateTimeOffset sinceUtc,
        CancellationToken cancellationToken = default) =>
        IsSqliteDatabase()
            ? CountMessagesBySenderSinceInMemoryAsync(senderMemberId, sinceUtc, cancellationToken)
            : dbContext.PrivateMessages
                .AsNoTracking()
                .CountAsync(
                    message => message.SenderMemberId == senderMemberId && message.CreatedAt >= sinceUtc,
                    cancellationToken);

    public Task<int> CountIdenticalMessagesBySenderSinceAsync(
        Guid senderMemberId,
        string body,
        DateTimeOffset sinceUtc,
        CancellationToken cancellationToken = default) =>
        IsSqliteDatabase()
            ? CountIdenticalMessagesBySenderSinceInMemoryAsync(senderMemberId, body, sinceUtc, cancellationToken)
            : dbContext.PrivateMessages
                .AsNoTracking()
                .CountAsync(
                    message => message.SenderMemberId == senderMemberId
                        && message.CreatedAt >= sinceUtc
                        && message.Body == body,
                    cancellationToken);

    public Task<int> CountDistinctNewRecipientsSinceAsync(
        Guid senderMemberId,
        DateTimeOffset sinceUtc,
        CancellationToken cancellationToken = default) =>
        IsSqliteDatabase()
            ? CountDistinctNewRecipientsSinceInMemoryAsync(senderMemberId, sinceUtc, cancellationToken)
            : dbContext.PrivateMessages
                .AsNoTracking()
                .Where(message => message.SenderMemberId == senderMemberId && message.CreatedAt >= sinceUtc)
                .Join(
                    dbContext.PrivateConversations.AsNoTracking().Where(c => c.CreatedAt >= sinceUtc),
                    message => message.ConversationId,
                    conversation => conversation.Id,
                    (message, conversation) => message.ConversationId)
                .Distinct()
                .CountAsync(cancellationToken);

    public async Task<PrivateMessageReportResult> CreateReportAsync(
        Guid reporterMemberId,
        Guid conversationId,
        Guid messageId,
        string? reason,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken = default)
    {
        var message = await dbContext.PrivateMessages
            .AsNoTracking()
            .SingleOrDefaultAsync(m => m.Id == messageId, cancellationToken);
        if (message is null || message.ConversationId != conversationId)
        {
            return new PrivateMessageReportResult(false, null, PrivateMessageReportText.MessageNotFound);
        }

        var isParticipant = await IsParticipantAsync(conversationId, reporterMemberId, cancellationToken);
        if (!isParticipant)
        {
            return new PrivateMessageReportResult(false, null, PrivateMessageReportText.NotAParticipant);
        }

        if (message.SenderMemberId == reporterMemberId)
        {
            return new PrivateMessageReportResult(false, null, PrivateMessageReportText.CannotReportOwn);
        }

        var existing = await dbContext.PrivateMessageReports
            .AsNoTracking()
            .SingleOrDefaultAsync(
                r => r.ReporterMemberId == reporterMemberId && r.MessageId == messageId,
                cancellationToken);
        if (existing is not null)
        {
            return new PrivateMessageReportResult(true, existing.Id, null, AlreadyReported: true);
        }

        var senderName = await dbContext.MemberAccounts
            .AsNoTracking()
            .Where(m => m.Id == message.SenderMemberId)
            .Select(m => m.DisplayName)
            .SingleOrDefaultAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(senderName))
        {
            senderName = "Unknown member";
        }

        var precedingRows = await dbContext.PrivateMessages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId && m.SortKey < message.SortKey)
            .OrderByDescending(m => m.SortKey)
            .Take(PrivateMessageLimits.ReportPrecedingMessageCount)
            .Select(m => new
            {
                m.Id,
                m.SenderMemberId,
                SenderName = m.Sender != null ? m.Sender.DisplayName : string.Empty,
                m.Body,
                m.CreatedAt,
                m.SortKey,
            })
            .ToListAsync(cancellationToken);
        var preceding = precedingRows
            .OrderBy(m => m.SortKey)
            .Select(m => new PrivateMessageReportContextItem(
                m.Id,
                m.SenderMemberId,
                string.IsNullOrWhiteSpace(m.SenderName) ? "Unknown member" : m.SenderName,
                m.Body,
                m.CreatedAt))
            .ToList();

        var entity = PrivateMessageReportMapping.CreateEntity(
            reporterMemberId,
            message,
            senderName,
            preceding,
            reason,
            createdAt);
        dbContext.PrivateMessageReports.Add(entity);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return new PrivateMessageReportResult(true, entity.Id, null);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            dbContext.ChangeTracker.Clear();
            var raced = await dbContext.PrivateMessageReports
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    r => r.ReporterMemberId == reporterMemberId && r.MessageId == messageId,
                    cancellationToken);
            if (raced is not null)
            {
                return new PrivateMessageReportResult(true, raced.Id, null, AlreadyReported: true);
            }

            throw;
        }
    }

    public async Task<PrivateMessageReport?> GetReportAsync(
        Guid reportId,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.PrivateMessageReports
            .AsNoTracking()
            .SingleOrDefaultAsync(r => r.Id == reportId, cancellationToken);
        return entity is null ? null : PrivateMessageReportMapping.ToModel(entity);
    }

    public async Task<IReadOnlySet<Guid>> GetReportedMessageIdsAsync(
        Guid conversationId,
        Guid reporterMemberId,
        CancellationToken cancellationToken = default)
    {
        var ids = await dbContext.PrivateMessageReports
            .AsNoTracking()
            .Where(r => r.ConversationId == conversationId && r.ReporterMemberId == reporterMemberId)
            .Select(r => r.MessageId)
            .ToListAsync(cancellationToken);
        return ids.ToHashSet();
    }

    public async Task<PrivateMessageReportListPage> ListReportsAsync(
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var statusFilter = NormalizeOptionalReportStatus(status);

        var query = dbContext.PrivateMessageReports.AsNoTracking();
        if (statusFilter is not null)
        {
            query = query.Where(r => r.Status == statusFilter);
        }

        if (IsSqliteDatabase())
        {
            var allRows = await query
                .Select(r => new PrivateMessageReportListItem(
                    r.Id,
                    r.MessageId,
                    r.ConversationId,
                    r.ReporterMemberId,
                    r.Reporter != null ? r.Reporter.DisplayName : "Unknown member",
                    r.ReportedMemberId,
                    r.Reported != null ? r.Reported.DisplayName : "Unknown member",
                    r.Reason,
                    r.Status,
                    r.CreatedAt))
                .ToListAsync(cancellationToken);

            var ordered = allRows.OrderByDescending(r => r.CreatedAt).ToList();
            var pagedItems = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return new PrivateMessageReportListPage(pagedItems, ordered.Count, statusFilter);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new PrivateMessageReportListItem(
                r.Id,
                r.MessageId,
                r.ConversationId,
                r.ReporterMemberId,
                r.Reporter != null ? r.Reporter.DisplayName : "Unknown member",
                r.ReportedMemberId,
                r.Reported != null ? r.Reported.DisplayName : "Unknown member",
                r.Reason,
                r.Status,
                r.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PrivateMessageReportListPage(items, totalCount, statusFilter);
    }

    public async Task<int> CountOpenReportsAsync(CancellationToken cancellationToken = default) =>
        await dbContext.PrivateMessageReports
            .AsNoTracking()
            .CountAsync(r => r.Status == PrivateMessageReportStatus.Open, cancellationToken);

    public async Task<PrivateMessageReport?> UpdateReportStatusAsync(
        Guid reportId,
        string status,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.PrivateMessageReports
            .SingleOrDefaultAsync(r => r.Id == reportId, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        var normalizedStatus = PrivateMessageReportStatus.Normalize(status);
        var previousStatus = entity.Status;
        entity.Status = normalizedStatus;

        if (!string.Equals(previousStatus, normalizedStatus, StringComparison.Ordinal))
        {
            dbContext.PrivateMessageReportAuditLogs.Add(new PrivateMessageReportAuditLogEntity
            {
                ReportId = reportId,
                Action = PrivateMessageReportAuditAction.StatusChanged,
                ActorEmail = actorEmail,
                OccurredAt = DateTimeOffset.UtcNow,
                Details = $"{previousStatus} -> {normalizedStatus}",
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return PrivateMessageReportMapping.ToModel(entity);
    }

    public async Task AppendReportViewedAuditAsync(
        Guid reportId,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        dbContext.PrivateMessageReportAuditLogs.Add(new PrivateMessageReportAuditLogEntity
        {
            ReportId = reportId,
            Action = PrivateMessageReportAuditAction.Viewed,
            ActorEmail = actorEmail,
            OccurredAt = DateTimeOffset.UtcNow,
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> PurgeExpiredReportsAsync(
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken = default)
    {
        var cutoff = asOfUtc - PrivateMessageLimits.ReportRetentionAfterTerminalStatus;

        var terminalReportIds = await dbContext.PrivateMessageReports
            .AsNoTracking()
            .Where(r => r.Status == PrivateMessageReportStatus.Dismissed
                || r.Status == PrivateMessageReportStatus.Actioned)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);
        if (terminalReportIds.Count == 0)
        {
            return 0;
        }

        // SQLite (tests) cannot translate MAX() over DateTimeOffset; materialise the relevant
        // audit rows and aggregate client-side. Production always targets SQL Server.
        var statusChangeRows = await dbContext.PrivateMessageReportAuditLogs
            .AsNoTracking()
            .Where(log =>
                log.Action == PrivateMessageReportAuditAction.StatusChanged
                && terminalReportIds.Contains(log.ReportId))
            .Select(log => new { log.ReportId, log.OccurredAt })
            .ToListAsync(cancellationToken);

        var eligibleIds = statusChangeRows
            .GroupBy(row => row.ReportId)
            .Where(g => g.Max(row => row.OccurredAt) <= cutoff)
            .Select(g => g.Key)
            .ToList();
        if (eligibleIds.Count == 0)
        {
            return 0;
        }

        return await dbContext.PrivateMessageReports
            .Where(r => eligibleIds.Contains(r.Id))
            .ExecuteDeleteAsync(cancellationToken);
    }

    private static string? NormalizeOptionalReportStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status) || string.Equals(status, "all", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return PrivateMessageReportStatus.Normalize(status);
    }

    private async Task<HashSet<Guid>> LoadReportedMessageIdsAsync(
        Guid conversationId,
        Guid reporterMemberId,
        IEnumerable<Guid> messageIds,
        CancellationToken cancellationToken)
    {
        var ids = messageIds.ToArray();
        if (ids.Length == 0)
        {
            return [];
        }

        var reported = await dbContext.PrivateMessageReports
            .AsNoTracking()
            .Where(r =>
                r.ConversationId == conversationId
                && r.ReporterMemberId == reporterMemberId
                && ids.Contains(r.MessageId))
            .Select(r => r.MessageId)
            .ToListAsync(cancellationToken);
        return reported.ToHashSet();
    }

    // SQLite fallback (also exercised in tests): the provider cannot translate DateTimeOffset
    // range comparisons, so materialise the sender's messages then filter by CreatedAt in
    // memory. Production always targets SQL Server; the efficient path above is covered by
    // tests/QueenZone.SqlServerTests (see docs/architecture/testing-policy.md).
    private async Task<int> CountMessagesBySenderSinceInMemoryAsync(
        Guid senderMemberId,
        DateTimeOffset sinceUtc,
        CancellationToken cancellationToken)
    {
        var createdAtValues = await dbContext.PrivateMessages
            .AsNoTracking()
            .Where(message => message.SenderMemberId == senderMemberId)
            .Select(message => message.CreatedAt)
            .ToListAsync(cancellationToken);
        return createdAtValues.Count(createdAt => createdAt >= sinceUtc);
    }

    private async Task<int> CountIdenticalMessagesBySenderSinceInMemoryAsync(
        Guid senderMemberId,
        string body,
        DateTimeOffset sinceUtc,
        CancellationToken cancellationToken)
    {
        var rows = await dbContext.PrivateMessages
            .AsNoTracking()
            .Where(message => message.SenderMemberId == senderMemberId && message.Body == body)
            .Select(message => message.CreatedAt)
            .ToListAsync(cancellationToken);
        return rows.Count(createdAt => createdAt >= sinceUtc);
    }

    private async Task<int> CountDistinctNewRecipientsSinceInMemoryAsync(
        Guid senderMemberId,
        DateTimeOffset sinceUtc,
        CancellationToken cancellationToken)
    {
        var senderRows = await dbContext.PrivateMessages
            .AsNoTracking()
            .Where(message => message.SenderMemberId == senderMemberId)
            .Select(message => new { message.ConversationId, message.CreatedAt })
            .ToListAsync(cancellationToken);

        var recentConversationIds = senderRows
            .Where(row => row.CreatedAt >= sinceUtc)
            .Select(row => row.ConversationId)
            .Distinct()
            .ToList();
        if (recentConversationIds.Count == 0)
        {
            return 0;
        }

        var conversationCreatedAtValues = await dbContext.PrivateConversations
            .AsNoTracking()
            .Where(conversation => recentConversationIds.Contains(conversation.Id))
            .Select(conversation => new { conversation.Id, conversation.CreatedAt })
            .ToListAsync(cancellationToken);

        return conversationCreatedAtValues.Count(row => row.CreatedAt >= sinceUtc);
    }

    private bool IsSqliteDatabase() =>
        string.Equals(
            dbContext.Database.ProviderName,
            "Microsoft.EntityFrameworkCore.Sqlite",
            StringComparison.Ordinal);

    internal static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        for (var inner = exception.InnerException; inner is not null; inner = inner.InnerException)
        {
            if (inner is SqlException sql && sql.Number is 2601 or 2627)
            {
                return true;
            }

            if (inner.Message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase)
                || inner.Message.Contains("unique index", StringComparison.OrdinalIgnoreCase)
                || inner.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static (Guid Low, Guid High) OrderPair(Guid a, Guid b) =>
        a.CompareTo(b) < 0 ? (a, b) : (b, a);

    private static string NormalizeBody(string body) =>
        string.IsNullOrWhiteSpace(body) ? string.Empty : body.Trim();

    private static string TruncatePreview(string body) =>
        body.Length <= PrivateMessageLimits.PreviewLength
            ? body
            : body[..PrivateMessageLimits.PreviewLength];

    private sealed record InboxPageRow(
        Guid ConversationId,
        long? LastReadSortKey,
        string Preview,
        DateTimeOffset LastMessageAt,
        string OtherDisplayName,
        Guid OtherId);

    private sealed record InboxPageRowWithUnread(
        Guid ConversationId,
        string Preview,
        DateTimeOffset LastMessageAt,
        string OtherDisplayName,
        Guid OtherId,
        int UnreadCount);

    private sealed record ConversationMessageRow(
        Guid Id,
        Guid SenderMemberId,
        string SenderName,
        string Body,
        DateTimeOffset CreatedAt,
        long SortKey);
}
