using QueenZone.Data.Entities;

namespace QueenZone.Data;

public sealed class InMemoryPrivateMessageRepository : IPrivateMessageRepository
{
    private readonly object sync = new();
    private readonly List<PrivateConversationEntity> conversations = [];
    private readonly List<PrivateConversationParticipantEntity> participants = [];
    private readonly List<PrivateMessageEntity> messages = [];
    private readonly Func<Guid, MemberAccount?>? resolveMember;

    public InMemoryPrivateMessageRepository(Func<Guid, MemberAccount?>? resolveMember = null)
    {
        this.resolveMember = resolveMember;
    }

    public Task<IReadOnlyList<PrivateConversationListItem>> GetInboxAsync(
        Guid memberId,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        lock (sync)
        {
            var mine = participants
                .Where(p => p.MemberId == memberId && !p.IsArchived)
                .Select(p => p.ConversationId)
                .ToHashSet();

            IReadOnlyList<PrivateConversationListItem> items = conversations
                .Where(c => mine.Contains(c.Id))
                .OrderByDescending(c => c.LastMessageAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => ToListItem(c, memberId))
                .ToList();

            return Task.FromResult(items);
        }
    }

    public Task<int> CountUnreadConversationsAsync(
        Guid memberId,
        CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            var count = participants
                .Where(p => p.MemberId == memberId && !p.IsArchived)
                .Count(p => CountUnread(p) > 0);
            return Task.FromResult(count);
        }
    }

    public Task<PrivateConversationDetail?> GetConversationAsync(
        Guid conversationId,
        Guid memberId,
        CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            var participant = participants.SingleOrDefault(
                p => p.ConversationId == conversationId && p.MemberId == memberId && !p.IsArchived);
            var conversation = conversations.SingleOrDefault(c => c.Id == conversationId);
            if (participant is null || conversation is null)
            {
                return Task.FromResult<PrivateConversationDetail?>(null);
            }

            var otherId = conversation.MemberLowId == memberId
                ? conversation.MemberHighId
                : conversation.MemberLowId;
            var otherName = resolveMember?.Invoke(otherId)?.DisplayName ?? "Unknown member";

            IReadOnlyList<PrivateMessageItem> items = messages
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.CreatedAt)
                .ThenBy(m => m.Id)
                .Select(m => new PrivateMessageItem(
                    m.Id,
                    m.SenderMemberId,
                    resolveMember?.Invoke(m.SenderMemberId)?.DisplayName ?? "Unknown member",
                    m.Body,
                    m.CreatedAt,
                    m.SenderMemberId == memberId))
                .ToList();

            return Task.FromResult<PrivateConversationDetail?>(
                new PrivateConversationDetail(conversationId, otherId, otherName, items));
        }
    }

    public Task<bool> IsParticipantAsync(
        Guid conversationId,
        Guid memberId,
        CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            return Task.FromResult(
                participants.Any(p => p.ConversationId == conversationId && p.MemberId == memberId));
        }
    }

    public Task MarkConversationReadAsync(
        Guid conversationId,
        Guid memberId,
        DateTimeOffset readAt,
        CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            var participant = participants.SingleOrDefault(
                p => p.ConversationId == conversationId && p.MemberId == memberId);
            if (participant is not null)
            {
                participant.LastReadAt = readAt;
            }

            return Task.CompletedTask;
        }
    }

    public Task<PrivateMessageSendResult> SendNewOrExistingAsync(
        Guid senderMemberId,
        Guid recipientMemberId,
        string body,
        DateTimeOffset sentAt,
        CancellationToken cancellationToken = default)
    {
        body = NormalizeBody(body);
        if (body.Length == 0)
        {
            return Task.FromResult(new PrivateMessageSendResult(false, null, "Message body is required."));
        }

        if (body.Length > PrivateMessageLimits.MaxBodyLength)
        {
            return Task.FromResult(new PrivateMessageSendResult(
                false,
                null,
                $"Message body must be {PrivateMessageLimits.MaxBodyLength} characters or fewer."));
        }

        if (senderMemberId == recipientMemberId)
        {
            return Task.FromResult(new PrivateMessageSendResult(false, null, "You cannot message yourself."));
        }

        lock (sync)
        {
            var (low, high) = OrderPair(senderMemberId, recipientMemberId);
            var conversation = conversations.SingleOrDefault(
                c => c.MemberLowId == low && c.MemberHighId == high);
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
                conversations.Add(conversation);
                participants.Add(new PrivateConversationParticipantEntity
                {
                    ConversationId = conversation.Id,
                    MemberId = senderMemberId,
                    LastReadAt = sentAt,
                    IsArchived = false,
                });
                participants.Add(new PrivateConversationParticipantEntity
                {
                    ConversationId = conversation.Id,
                    MemberId = recipientMemberId,
                    LastReadAt = null,
                    IsArchived = false,
                });
            }
            else
            {
                EnsureParticipant(conversation.Id, senderMemberId, sentAt);
                EnsureParticipant(conversation.Id, recipientMemberId, lastReadAt: null);
                conversation.LastMessageAt = sentAt;
                conversation.LastMessagePreview = TruncatePreview(body);
                conversation.LastMessageSenderId = senderMemberId;
                Unarchive(conversation.Id, senderMemberId);
                Unarchive(conversation.Id, recipientMemberId);
                var senderParticipant = participants.Single(
                    p => p.ConversationId == conversation.Id && p.MemberId == senderMemberId);
                senderParticipant.LastReadAt = sentAt;
            }

            messages.Add(new PrivateMessageEntity
            {
                Id = Guid.NewGuid(),
                ConversationId = conversation.Id,
                SenderMemberId = senderMemberId,
                Body = body,
                CreatedAt = sentAt,
            });

            return Task.FromResult(new PrivateMessageSendResult(true, conversation.Id, null));
        }
    }

    public Task<PrivateMessageSendResult> ReplyAsync(
        Guid conversationId,
        Guid senderMemberId,
        string body,
        DateTimeOffset sentAt,
        CancellationToken cancellationToken = default)
    {
        body = NormalizeBody(body);
        if (body.Length == 0)
        {
            return Task.FromResult(new PrivateMessageSendResult(false, null, "Message body is required."));
        }

        if (body.Length > PrivateMessageLimits.MaxBodyLength)
        {
            return Task.FromResult(new PrivateMessageSendResult(
                false,
                null,
                $"Message body must be {PrivateMessageLimits.MaxBodyLength} characters or fewer."));
        }

        lock (sync)
        {
            if (!participants.Any(p => p.ConversationId == conversationId && p.MemberId == senderMemberId))
            {
                return Task.FromResult(new PrivateMessageSendResult(
                    false,
                    null,
                    "You are not a participant in this conversation."));
            }

            var conversation = conversations.SingleOrDefault(c => c.Id == conversationId);
            if (conversation is null)
            {
                return Task.FromResult(new PrivateMessageSendResult(false, null, "Conversation not found."));
            }

            messages.Add(new PrivateMessageEntity
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

            Unarchive(conversationId, conversation.MemberLowId);
            Unarchive(conversationId, conversation.MemberHighId);

            var senderParticipant = participants.Single(
                p => p.ConversationId == conversationId && p.MemberId == senderMemberId);
            senderParticipant.LastReadAt = sentAt;

            return Task.FromResult(new PrivateMessageSendResult(true, conversationId, null));
        }
    }

    private PrivateConversationListItem ToListItem(PrivateConversationEntity conversation, Guid memberId)
    {
        var otherId = conversation.MemberLowId == memberId
            ? conversation.MemberHighId
            : conversation.MemberLowId;
        var otherName = resolveMember?.Invoke(otherId)?.DisplayName ?? "Unknown member";
        var participant = participants.Single(p => p.ConversationId == conversation.Id && p.MemberId == memberId);
        var unread = CountUnread(participant);
        return new PrivateConversationListItem(
            conversation.Id,
            otherId,
            otherName,
            conversation.LastMessagePreview,
            conversation.LastMessageAt,
            unread > 0,
            unread);
    }

    private int CountUnread(PrivateConversationParticipantEntity participant)
    {
        return messages.Count(m =>
            m.ConversationId == participant.ConversationId
            && m.SenderMemberId != participant.MemberId
            && (participant.LastReadAt is null || m.CreatedAt > participant.LastReadAt));
    }

    private void EnsureParticipant(Guid conversationId, Guid memberId, DateTimeOffset? lastReadAt)
    {
        if (participants.Any(p => p.ConversationId == conversationId && p.MemberId == memberId))
        {
            return;
        }

        participants.Add(new PrivateConversationParticipantEntity
        {
            ConversationId = conversationId,
            MemberId = memberId,
            LastReadAt = lastReadAt,
            IsArchived = false,
        });
    }

    private void Unarchive(Guid conversationId, Guid memberId)
    {
        var participant = participants.SingleOrDefault(
            p => p.ConversationId == conversationId && p.MemberId == memberId);
        if (participant is not null)
        {
            participant.IsArchived = false;
        }
    }

    private static (Guid Low, Guid High) OrderPair(Guid a, Guid b) =>
        a.CompareTo(b) < 0 ? (a, b) : (b, a);

    private static string NormalizeBody(string body) =>
        string.IsNullOrWhiteSpace(body) ? string.Empty : body.Trim();

    private static string TruncatePreview(string body)
    {
        if (body.Length <= PrivateMessageLimits.PreviewLength)
        {
            return body;
        }

        return body[..PrivateMessageLimits.PreviewLength];
    }
}
