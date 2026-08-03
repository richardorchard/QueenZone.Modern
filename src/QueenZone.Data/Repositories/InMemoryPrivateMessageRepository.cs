using QueenZone.Data.Entities;

namespace QueenZone.Data;

public sealed class InMemoryPrivateMessageRepository : IPrivateMessageRepository
{
    private readonly object sync = new();
    private readonly List<PrivateConversationEntity> conversations = [];
    private readonly List<PrivateConversationParticipantEntity> participants = [];
    private readonly List<PrivateMessageEntity> messages = [];
    private readonly Func<Guid, MemberAccount?>? resolveMember;
    private long nextSortKey = 1;

    public InMemoryPrivateMessageRepository(Func<Guid, MemberAccount?>? resolveMember = null)
    {
        this.resolveMember = resolveMember;
    }

    public Task<PrivateInboxPage> GetInboxAsync(
        Guid memberId,
        int page = 1,
        int pageSize = PrivateMessageLimits.InboxPageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, PrivateMessageLimits.MaxInboxPageSize);

        lock (sync)
        {
            var mine = participants
                .Where(p => p.MemberId == memberId && !p.IsArchived && !p.IsRemoved)
                .Select(p => p.ConversationId)
                .ToHashSet();

            var ordered = conversations
                .Where(c => mine.Contains(c.Id))
                .OrderByDescending(c => c.LastMessageSortKey)
                .ThenByDescending(c => c.Id)
                .ToList();
            var totalCount = ordered.Count;
            var totalPages = totalCount <= 0 ? 1 : (totalCount + pageSize - 1) / pageSize;
            page = Math.Min(page, totalPages);

            IReadOnlyList<PrivateConversationListItem> items = ordered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => ToListItem(c, memberId))
                .ToList();

            return Task.FromResult(new PrivateInboxPage(items, totalCount, page, pageSize));
        }
    }

    public Task<int> CountUnreadConversationsAsync(
        Guid memberId,
        CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            var count = participants
                .Where(p => p.MemberId == memberId && !p.IsArchived && !p.IsRemoved)
                .Count(p => CountUnread(p) > 0);
            return Task.FromResult(count);
        }
    }

    public Task<PrivateConversationDetail?> GetConversationAsync(
        Guid conversationId,
        Guid memberId,
        int? page = null,
        int pageSize = PrivateMessageLimits.ConversationPageSize,
        CancellationToken cancellationToken = default)
    {
        pageSize = Math.Clamp(pageSize, 1, PrivateMessageLimits.MaxConversationPageSize);

        lock (sync)
        {
            var participant = participants.SingleOrDefault(
                p => p.ConversationId == conversationId && p.MemberId == memberId);
            var conversation = conversations.SingleOrDefault(c => c.Id == conversationId);
            if (participant is null || conversation is null)
            {
                return Task.FromResult<PrivateConversationDetail?>(null);
            }

            var otherId = conversation.MemberLowId == memberId
                ? conversation.MemberHighId
                : conversation.MemberLowId;
            var otherName = resolveMember?.Invoke(otherId)?.DisplayName ?? "Unknown member";

            var ordered = messages
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.SortKey)
                .ToList();
            var totalCount = ordered.Count;
            var totalPages = totalCount <= 0
                ? 1
                : (totalCount + pageSize - 1) / pageSize;

            var effectivePage = page is null or < 1
                ? totalPages
                : Math.Min(page.Value, totalPages);
            var useLatestWindow = page is null or < 1 || effectivePage >= totalPages;

            List<PrivateMessageEntity> pageMessages;
            if (useLatestWindow)
            {
                pageMessages = ordered
                    .OrderByDescending(m => m.SortKey)
                    .Take(pageSize)
                    .OrderBy(m => m.SortKey)
                    .ToList();
                effectivePage = totalPages;
            }
            else
            {
                pageMessages = ordered
                    .Skip((effectivePage - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
            }

            IReadOnlyList<PrivateMessageItem> items = pageMessages
                .Select(m => new PrivateMessageItem(
                    m.Id,
                    m.SenderMemberId,
                    resolveMember?.Invoke(m.SenderMemberId)?.DisplayName ?? "Unknown member",
                    m.Body,
                    m.CreatedAt,
                    m.SenderMemberId == memberId,
                    m.SortKey))
                .ToList();

            return Task.FromResult<PrivateConversationDetail?>(
                new PrivateConversationDetail(
                    conversationId,
                    otherId,
                    otherName,
                    items,
                    totalCount,
                    effectivePage,
                    pageSize));
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
        long lastReadSortKey,
        DateTimeOffset readAt,
        CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            var participant = participants.SingleOrDefault(
                p => p.ConversationId == conversationId && p.MemberId == memberId);
            if (participant is null)
            {
                return Task.CompletedTask;
            }

            if (participant.LastReadSortKey is null || participant.LastReadSortKey < lastReadSortKey)
            {
                participant.LastReadSortKey = lastReadSortKey;
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
            var isNew = conversation is null;
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
                    LastMessagePreview = TruncatePreview(body),
                    LastMessageSenderId = senderMemberId,
                };
                conversations.Add(conversation);
                participants.Add(new PrivateConversationParticipantEntity
                {
                    ConversationId = conversation.Id,
                    MemberId = senderMemberId,
                    LastReadAt = sentAt,
                    LastReadSortKey = null,
                    IsArchived = false,
                    IsRemoved = false,
                });
                participants.Add(new PrivateConversationParticipantEntity
                {
                    ConversationId = conversation.Id,
                    MemberId = recipientMemberId,
                    LastReadAt = null,
                    LastReadSortKey = null,
                    IsArchived = false,
                    IsRemoved = false,
                });
            }
            else
            {
                EnsureParticipant(conversation.Id, senderMemberId);
                EnsureParticipant(conversation.Id, recipientMemberId);
                ReactivateParticipant(conversation.Id, senderMemberId);
                ReactivateParticipant(conversation.Id, recipientMemberId);
            }

            var message = new PrivateMessageEntity
            {
                Id = Guid.NewGuid(),
                ConversationId = conversation.Id,
                SenderMemberId = senderMemberId,
                Body = body,
                CreatedAt = sentAt,
                SortKey = nextSortKey++,
            };
            messages.Add(message);

            if (isNew)
            {
                conversation.LastMessageSortKey = message.SortKey;
                var sender = participants.Single(
                    p => p.ConversationId == conversation.Id && p.MemberId == senderMemberId);
                sender.LastReadSortKey = message.SortKey;
                sender.LastReadAt = sentAt;
            }
            else
            {
                ApplySummaryForInsertedMessage(
                    conversation,
                    sentAt,
                    TruncatePreview(body),
                    senderMemberId,
                    message.SortKey);
            }

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

            var message = new PrivateMessageEntity
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                SenderMemberId = senderMemberId,
                Body = body,
                CreatedAt = sentAt,
                SortKey = nextSortKey++,
            };
            messages.Add(message);

            ApplySummaryForInsertedMessage(
                conversation,
                sentAt,
                TruncatePreview(body),
                senderMemberId,
                message.SortKey);
            ReactivateParticipant(conversationId, conversation.MemberLowId);
            ReactivateParticipant(conversationId, conversation.MemberHighId);

            return Task.FromResult(new PrivateMessageSendResult(true, conversationId, null));
        }
    }

    public Task<bool> RemoveConversationAsync(
        Guid conversationId,
        Guid memberId,
        CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            var participant = participants.SingleOrDefault(
                p => p.ConversationId == conversationId && p.MemberId == memberId);
            if (participant is null)
            {
                return Task.FromResult(false);
            }

            participant.IsRemoved = true;
            return Task.FromResult(true);
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
            && (participant.LastReadSortKey is null || m.SortKey > participant.LastReadSortKey));
    }

    private void EnsureParticipant(Guid conversationId, Guid memberId)
    {
        if (participants.Any(p => p.ConversationId == conversationId && p.MemberId == memberId))
        {
            return;
        }

        participants.Add(new PrivateConversationParticipantEntity
        {
            ConversationId = conversationId,
            MemberId = memberId,
            LastReadAt = null,
            LastReadSortKey = null,
            IsArchived = false,
            IsRemoved = false,
        });
    }

    private void ReactivateParticipant(Guid conversationId, Guid memberId)
    {
        var participant = participants.SingleOrDefault(
            p => p.ConversationId == conversationId && p.MemberId == memberId);
        if (participant is not null)
        {
            participant.IsArchived = false;
            participant.IsRemoved = false;
        }
    }

    private static void ApplySummaryForInsertedMessage(
        PrivateConversationEntity conversation,
        DateTimeOffset sentAt,
        string preview,
        Guid senderMemberId,
        long sortKey)
    {
        if (sentAt > conversation.LastMessageAt)
        {
            conversation.LastMessageAt = sentAt;
        }

        conversation.LastMessageSortKey = sortKey;
        conversation.LastMessagePreview = preview;
        conversation.LastMessageSenderId = senderMemberId;
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
