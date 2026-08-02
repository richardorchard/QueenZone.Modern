using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using QueenZone.Data.Entities;

namespace QueenZone.Data;

public sealed class EfPrivateMessageRepository(QueenZoneDbContext dbContext) : IPrivateMessageRepository
{
    private const int MaxUniqueConflictRetries = 3;

    public async Task<IReadOnlyList<PrivateConversationListItem>> GetInboxAsync(
        Guid memberId,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        if (IsSqliteDatabase())
        {
            return await GetInboxSqliteAsync(memberId, page, pageSize, cancellationToken);
        }

        var pageRows = await dbContext.PrivateConversationParticipants
            .AsNoTracking()
            .Where(p => p.MemberId == memberId && !p.IsArchived)
            .OrderByDescending(p => p.Conversation!.LastMessageAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new InboxPageRow(
                p.ConversationId,
                p.LastReadSortKey,
                p.Conversation!.LastMessagePreview,
                p.Conversation.LastMessageAt,
                p.Conversation.MemberLowId == memberId
                    ? (p.Conversation.MemberHigh != null ? p.Conversation.MemberHigh.DisplayName : string.Empty)
                    : (p.Conversation.MemberLow != null ? p.Conversation.MemberLow.DisplayName : string.Empty),
                p.Conversation.MemberLowId == memberId
                    ? p.Conversation.MemberHighId
                    : p.Conversation.MemberLowId))
            .ToListAsync(cancellationToken);

        var unreadByConversation = await CountUnreadForPageSqlAsync(memberId, pageRows, cancellationToken);
        return MapInbox(pageRows, unreadByConversation);
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

        var participant = await dbContext.PrivateConversationParticipants
            .AsNoTracking()
            .SingleOrDefaultAsync(
                p => p.ConversationId == conversationId && p.MemberId == memberId,
                cancellationToken);
        if (participant is null)
        {
            return null;
        }

        var conversation = await dbContext.PrivateConversations
            .AsNoTracking()
            .Include(c => c.MemberLow)
            .Include(c => c.MemberHigh)
            .SingleOrDefaultAsync(c => c.Id == conversationId, cancellationToken);
        if (conversation is null)
        {
            return null;
        }

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
        var effectivePage = page is null or < 1
            ? totalPages
            : Math.Min(page.Value, totalPages);

        var messageRows = await dbContext.PrivateMessages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.SortKey)
            .Skip((effectivePage - 1) * pageSize)
            .Take(pageSize)
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

        IReadOnlyList<PrivateMessageItem> items = messageRows
            .Select(m => new PrivateMessageItem(
                m.Id,
                m.SenderMemberId,
                string.IsNullOrWhiteSpace(m.SenderName) ? "Unknown member" : m.SenderName,
                m.Body,
                m.CreatedAt,
                m.SenderMemberId == memberId,
                m.SortKey))
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

                await UnarchiveAsync(conversationId, conversation.MemberLowId, cancellationToken);
                await UnarchiveAsync(conversationId, conversation.MemberHighId, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);

                await UpdateConversationSummaryIfNewerAsync(
                    conversationId,
                    sentAt,
                    preview,
                    senderMemberId,
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
                    });
                    dbContext.PrivateConversationParticipants.Add(new PrivateConversationParticipantEntity
                    {
                        ConversationId = conversation.Id,
                        MemberId = recipientMemberId,
                        LastReadAt = null,
                        LastReadSortKey = null,
                        IsArchived = false,
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

                    await transaction.CommitAsync(cancellationToken);
                    return conversation.Id;
                }

                var conversationId = conversation.Id;

                await EnsureParticipantAsync(conversationId, senderMemberId, cancellationToken);
                await EnsureParticipantAsync(conversationId, recipientMemberId, cancellationToken);
                await UnarchiveAsync(conversationId, senderMemberId, cancellationToken);
                await UnarchiveAsync(conversationId, recipientMemberId, cancellationToken);

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

                await UpdateConversationSummaryIfNewerAsync(
                    conversationId,
                    sentAt,
                    preview,
                    senderMemberId,
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

    private async Task UpdateConversationSummaryIfNewerAsync(
        Guid conversationId,
        DateTimeOffset sentAt,
        string preview,
        Guid senderMemberId,
        CancellationToken cancellationToken)
    {
        if (IsSqliteDatabase())
        {
            var conversation = await dbContext.PrivateConversations
                .SingleOrDefaultAsync(c => c.Id == conversationId, cancellationToken);
            if (conversation is null || conversation.LastMessageAt > sentAt)
            {
                return;
            }

            conversation.LastMessageAt = sentAt;
            conversation.LastMessagePreview = preview;
            conversation.LastMessageSenderId = senderMemberId;
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        await dbContext.PrivateConversations
            .Where(c => c.Id == conversationId && c.LastMessageAt <= sentAt)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(c => c.LastMessageAt, sentAt)
                    .SetProperty(c => c.LastMessagePreview, preview)
                    .SetProperty(c => c.LastMessageSenderId, senderMemberId),
                cancellationToken);
    }

    private async Task<int> CountUnreadConversationsSqlAsync(
        Guid memberId,
        CancellationToken cancellationToken)
    {
        return await dbContext.PrivateConversationParticipants
            .AsNoTracking()
            .Where(p => p.MemberId == memberId && !p.IsArchived)
            .Where(p => dbContext.PrivateMessages.Any(m =>
                m.ConversationId == p.ConversationId
                && m.SenderMemberId != memberId
                && (p.LastReadSortKey == null || m.SortKey > p.LastReadSortKey)))
            .CountAsync(cancellationToken);
    }

    private async Task<int> CountUnreadConversationsSqliteAsync(
        Guid memberId,
        CancellationToken cancellationToken)
    {
        var pageRows = await dbContext.PrivateConversationParticipants
            .AsNoTracking()
            .Where(p => p.MemberId == memberId && !p.IsArchived)
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

    private async Task<IReadOnlyList<PrivateConversationListItem>> GetInboxSqliteAsync(
        Guid memberId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var rows = await dbContext.PrivateConversationParticipants
            .AsNoTracking()
            .Where(p => p.MemberId == memberId && !p.IsArchived)
            .Select(p => new
            {
                p.ConversationId,
                p.LastReadSortKey,
                Preview = p.Conversation!.LastMessagePreview,
                LastMessageAt = p.Conversation.LastMessageAt,
                OtherDisplayName = p.Conversation.MemberLowId == memberId
                    ? (p.Conversation.MemberHigh != null ? p.Conversation.MemberHigh.DisplayName : string.Empty)
                    : (p.Conversation.MemberLow != null ? p.Conversation.MemberLow.DisplayName : string.Empty),
                OtherId = p.Conversation.MemberLowId == memberId
                    ? p.Conversation.MemberHighId
                    : p.Conversation.MemberLowId,
            })
            .ToListAsync(cancellationToken);

        var pageRows = rows
            .OrderByDescending(r => r.LastMessageAt)
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
        return MapInbox(pageRows, unreadByConversation);
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
        });
    }

    private async Task UnarchiveAsync(
        Guid conversationId,
        Guid memberId,
        CancellationToken cancellationToken)
    {
        var participant = await dbContext.PrivateConversationParticipants
            .SingleOrDefaultAsync(
                p => p.ConversationId == conversationId && p.MemberId == memberId,
                cancellationToken);
        if (participant is not null)
        {
            participant.IsArchived = false;
        }
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
}
