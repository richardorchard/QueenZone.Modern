using Microsoft.EntityFrameworkCore;
using QueenZone.Data.Entities;

namespace QueenZone.Data;

public sealed class EfPrivateMessageRepository(QueenZoneDbContext dbContext) : IPrivateMessageRepository
{
    public async Task<IReadOnlyList<PrivateConversationListItem>> GetInboxAsync(
        Guid memberId,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var rows = await dbContext.PrivateConversationParticipants
            .AsNoTracking()
            .Where(p => p.MemberId == memberId && !p.IsArchived)
            .Select(p => new
            {
                p.ConversationId,
                p.LastReadAt,
                Conversation = p.Conversation!,
                OtherDisplayName = p.Conversation!.MemberLowId == memberId
                    ? (p.Conversation.MemberHigh != null ? p.Conversation.MemberHigh.DisplayName : string.Empty)
                    : (p.Conversation.MemberLow != null ? p.Conversation.MemberLow.DisplayName : string.Empty),
                OtherId = p.Conversation!.MemberLowId == memberId
                    ? p.Conversation.MemberHighId
                    : p.Conversation.MemberLowId,
            })
            .ToListAsync(cancellationToken);

        var conversationIds = rows.Select(r => r.ConversationId).ToList();
        var unreadByConversation = await CountUnreadByConversationAsync(
            memberId,
            conversationIds,
            rows.ToDictionary(r => r.ConversationId, r => r.LastReadAt),
            cancellationToken);

        return rows
            .OrderByDescending(r => r.Conversation.LastMessageAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r =>
            {
                var unread = unreadByConversation.GetValueOrDefault(r.ConversationId);
                return new PrivateConversationListItem(
                    r.ConversationId,
                    r.OtherId,
                    string.IsNullOrWhiteSpace(r.OtherDisplayName) ? "Unknown member" : r.OtherDisplayName,
                    r.Conversation.LastMessagePreview,
                    r.Conversation.LastMessageAt,
                    unread > 0,
                    unread);
            })
            .ToList();
    }

    public async Task<int> CountUnreadConversationsAsync(
        Guid memberId,
        CancellationToken cancellationToken = default)
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

        var unread = await CountUnreadByConversationAsync(
            memberId,
            participants.Select(p => p.ConversationId).ToList(),
            participants.ToDictionary(p => p.ConversationId, p => p.LastReadAt),
            cancellationToken);

        return unread.Count(pair => pair.Value > 0);
    }

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

        participant.LastReadAt = readAt;
        await dbContext.SaveChangesAsync(cancellationToken);
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
                LastMessagePreview = TruncatePreview(body),
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
        }
        else
        {
            conversation.LastMessageAt = sentAt;
            conversation.LastMessagePreview = TruncatePreview(body);
            conversation.LastMessageSenderId = senderMemberId;
            await EnsureParticipantAsync(conversation.Id, senderMemberId, sentAt, cancellationToken);
            await EnsureParticipantAsync(conversation.Id, recipientMemberId, null, cancellationToken);
            await UnarchiveAsync(conversation.Id, senderMemberId, cancellationToken);
            await UnarchiveAsync(conversation.Id, recipientMemberId, cancellationToken);

            var senderParticipant = await dbContext.PrivateConversationParticipants
                .SingleAsync(
                    p => p.ConversationId == conversation.Id && p.MemberId == senderMemberId,
                    cancellationToken);
            senderParticipant.LastReadAt = sentAt;
        }

        dbContext.PrivateMessages.Add(new PrivateMessageEntity
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            SenderMemberId = senderMemberId,
            Body = body,
            CreatedAt = sentAt,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return new PrivateMessageSendResult(true, conversation.Id, null);
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
            .SingleOrDefaultAsync(c => c.Id == conversationId, cancellationToken);
        if (conversation is null)
        {
            return new PrivateMessageSendResult(false, null, "Conversation not found.");
        }

        dbContext.PrivateMessages.Add(new PrivateMessageEntity
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderMemberId = senderMemberId,
            Body = body,
            CreatedAt = sentAt,
        });

        conversation.LastMessageAt = sentAt;
        conversation.LastMessagePreview = TruncatePreview(body);
        conversation.LastMessageSenderId = senderMemberId;

        await UnarchiveAsync(conversationId, conversation.MemberLowId, cancellationToken);
        await UnarchiveAsync(conversationId, conversation.MemberHighId, cancellationToken);

        var senderParticipant = await dbContext.PrivateConversationParticipants
            .SingleAsync(
                p => p.ConversationId == conversationId && p.MemberId == senderMemberId,
                cancellationToken);
        senderParticipant.LastReadAt = sentAt;

        await dbContext.SaveChangesAsync(cancellationToken);
        return new PrivateMessageSendResult(true, conversationId, null);
    }

    private async Task<Dictionary<Guid, int>> CountUnreadByConversationAsync(
        Guid memberId,
        IReadOnlyList<Guid> conversationIds,
        IReadOnlyDictionary<Guid, DateTimeOffset?> lastReadByConversation,
        CancellationToken cancellationToken)
    {
        if (conversationIds.Count == 0)
        {
            return [];
        }

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

    private static (Guid Low, Guid High) OrderPair(Guid a, Guid b) =>
        a.CompareTo(b) < 0 ? (a, b) : (b, a);

    private static string NormalizeBody(string body) =>
        string.IsNullOrWhiteSpace(body) ? string.Empty : body.Trim();

    private static string TruncatePreview(string body) =>
        body.Length <= PrivateMessageLimits.PreviewLength
            ? body
            : body[..PrivateMessageLimits.PreviewLength];
}
