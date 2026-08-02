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

        // SQLite EF cannot reliably translate DateTimeOffset ORDER BY in paged queries.
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
            .Select(p => new
            {
                p.ConversationId,
                p.LastReadAt,
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

        var unreadByConversation = await CountUnreadForPageAsync(
            memberId,
            pageRows.Select(r => new ConversationReadCursor(r.ConversationId, r.LastReadAt)).ToList(),
            cancellationToken);

        return pageRows
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
        CancellationToken cancellationToken = default)
    {
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

        var messageRows = await dbContext.PrivateMessages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId)
            .Select(m => new
            {
                m.Id,
                m.SenderMemberId,
                SenderName = m.Sender != null ? m.Sender.DisplayName : string.Empty,
                m.Body,
                m.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        IReadOnlyList<PrivateMessageItem> items = messageRows
            .OrderBy(m => m.CreatedAt)
            .ThenBy(m => m.Id)
            .Select(m => new PrivateMessageItem(
                m.Id,
                m.SenderMemberId,
                string.IsNullOrWhiteSpace(m.SenderName) ? "Unknown member" : m.SenderName,
                m.Body,
                m.CreatedAt,
                m.SenderMemberId == memberId))
            .ToList();

        return new PrivateConversationDetail(
            conversationId,
            otherId,
            string.IsNullOrWhiteSpace(otherName) ? "Unknown member" : otherName,
            items);
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
        DateTimeOffset readAt,
        CancellationToken cancellationToken = default)
    {
        var participant = await dbContext.PrivateConversationParticipants
            .SingleOrDefaultAsync(
                p => p.ConversationId == conversationId && p.MemberId == memberId,
                cancellationToken);
        if (participant is null)
        {
            return;
        }

        // Never move the cursor backwards (e.g. concurrent opens).
        if (participant.LastReadAt is null || readAt > participant.LastReadAt)
        {
            participant.LastReadAt = readAt;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
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
                // Concurrent first-send raced on the member-pair unique index. Clear tracked
                // inserts and retry — the next pass will find the winning conversation.
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

        var conversation = await dbContext.PrivateConversations
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == conversationId, cancellationToken);
        if (conversation is null)
        {
            return new PrivateMessageSendResult(false, null, "Conversation not found.");
        }

        var preview = TruncatePreview(body);
        dbContext.PrivateMessages.Add(new PrivateMessageEntity
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderMemberId = senderMemberId,
            Body = body,
            CreatedAt = sentAt,
        });

        var senderParticipant = await dbContext.PrivateConversationParticipants
            .SingleAsync(
                p => p.ConversationId == conversationId && p.MemberId == senderMemberId,
                cancellationToken);
        if (senderParticipant.LastReadAt is null || sentAt > senderParticipant.LastReadAt)
        {
            senderParticipant.LastReadAt = sentAt;
        }

        await UnarchiveAsync(conversationId, conversation.MemberLowId, cancellationToken);
        await UnarchiveAsync(conversationId, conversation.MemberHighId, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Conditional summary update so an earlier reply that commits later cannot overwrite
        // a newer preview/order established by a concurrent later reply.
        await UpdateConversationSummaryIfNewerAsync(conversationId, sentAt, preview, senderMemberId, cancellationToken);

        return new PrivateMessageSendResult(true, conversationId, null);
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
        var conversation = await dbContext.PrivateConversations
            .SingleOrDefaultAsync(c => c.MemberLowId == low && c.MemberHighId == high, cancellationToken);

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
                IsArchived = false,
            });
            dbContext.PrivateConversationParticipants.Add(new PrivateConversationParticipantEntity
            {
                ConversationId = conversation.Id,
                MemberId = recipientMemberId,
                LastReadAt = null,
                IsArchived = false,
            });

            dbContext.PrivateMessages.Add(new PrivateMessageEntity
            {
                Id = Guid.NewGuid(),
                ConversationId = conversation.Id,
                SenderMemberId = senderMemberId,
                Body = body,
                CreatedAt = sentAt,
            });

            await dbContext.SaveChangesAsync(cancellationToken);
            return conversation.Id;
        }

        var conversationId = conversation.Id;
        // Detach the tracked conversation — summary is updated conditionally after insert.
        dbContext.Entry(conversation).State = EntityState.Detached;

        await EnsureParticipantAsync(conversationId, senderMemberId, sentAt, cancellationToken);
        await EnsureParticipantAsync(conversationId, recipientMemberId, null, cancellationToken);
        await UnarchiveAsync(conversationId, senderMemberId, cancellationToken);
        await UnarchiveAsync(conversationId, recipientMemberId, cancellationToken);

        var senderParticipant = await dbContext.PrivateConversationParticipants
            .SingleAsync(
                p => p.ConversationId == conversationId && p.MemberId == senderMemberId,
                cancellationToken);
        if (senderParticipant.LastReadAt is null || sentAt > senderParticipant.LastReadAt)
        {
            senderParticipant.LastReadAt = sentAt;
        }

        dbContext.PrivateMessages.Add(new PrivateMessageEntity
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderMemberId = senderMemberId,
            Body = body,
            CreatedAt = sentAt,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        await UpdateConversationSummaryIfNewerAsync(
            conversationId,
            sentAt,
            preview,
            senderMemberId,
            cancellationToken);
        return conversationId;
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
            // SQLite EF cannot translate DateTimeOffset comparisons inside ExecuteUpdate.
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
        // SQL aggregate: count non-archived conversations with at least one unread message.
        return await dbContext.PrivateConversationParticipants
            .AsNoTracking()
            .Where(p => p.MemberId == memberId && !p.IsArchived)
            .Where(p => dbContext.PrivateMessages.Any(m =>
                m.ConversationId == p.ConversationId
                && m.SenderMemberId != memberId
                && (p.LastReadAt == null || m.CreatedAt > p.LastReadAt)))
            .CountAsync(cancellationToken);
    }

    private async Task<int> CountUnreadConversationsSqliteAsync(
        Guid memberId,
        CancellationToken cancellationToken)
    {
        var participants = await dbContext.PrivateConversationParticipants
            .AsNoTracking()
            .Where(p => p.MemberId == memberId && !p.IsArchived)
            .Select(p => new { p.ConversationId, p.LastReadAt })
            .ToListAsync(cancellationToken);
        if (participants.Count == 0)
        {
            return 0;
        }

        var unread = await CountUnreadForPageAsync(
            memberId,
            participants.Select(p => new ConversationReadCursor(p.ConversationId, p.LastReadAt)).ToList(),
            cancellationToken);
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
                p.LastReadAt,
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
            .ToList();

        var unreadByConversation = await CountUnreadForPageAsync(
            memberId,
            pageRows.Select(r => new ConversationReadCursor(r.ConversationId, r.LastReadAt)).ToList(),
            cancellationToken);

        return pageRows
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
    }

    private async Task<Dictionary<Guid, int>> CountUnreadForPageAsync(
        Guid memberId,
        IReadOnlyList<ConversationReadCursor> conversations,
        CancellationToken cancellationToken)
    {
        if (conversations.Count == 0)
        {
            return [];
        }

        var conversationIds = conversations.Select(c => c.ConversationId).ToList();
        var lastReadByConversation = conversations.ToDictionary(c => c.ConversationId, c => c.LastReadAt);

        // Only load unread candidates for the current inbox page, not the full history.
        var messageRows = await dbContext.PrivateMessages
            .AsNoTracking()
            .Where(m => conversationIds.Contains(m.ConversationId) && m.SenderMemberId != memberId)
            .Select(m => new { m.ConversationId, m.CreatedAt })
            .ToListAsync(cancellationToken);

        var result = conversationIds.ToDictionary(id => id, _ => 0);
        foreach (var message in messageRows)
        {
            var lastRead = lastReadByConversation.GetValueOrDefault(message.ConversationId);
            if (lastRead is null || message.CreatedAt > lastRead)
            {
                result[message.ConversationId] = result.GetValueOrDefault(message.ConversationId) + 1;
            }
        }

        return result;
    }

    private async Task EnsureParticipantAsync(
        Guid conversationId,
        Guid memberId,
        DateTimeOffset? lastReadAt,
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
            LastReadAt = lastReadAt,
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

            // SQLite (and provider wrappers) surface uniqueness as text; Data project does not
            // reference Microsoft.Data.Sqlite, so detect by message rather than exception type.
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

    private sealed record ConversationReadCursor(Guid ConversationId, DateTimeOffset? LastReadAt);
}
