using QueenZone.Data.Entities;

namespace QueenZone.Data;

public sealed class InMemoryPrivateMessageRepository : IPrivateMessageRepository, IPrivateMessageReportReviewRepository
{
    private readonly object sync = new();
    private readonly List<PrivateConversationEntity> conversations = [];
    private readonly List<PrivateConversationParticipantEntity> participants = [];
    private readonly List<PrivateMessageEntity> messages = [];
    private readonly List<PrivateMessageReportEntity> reports = [];
    private readonly List<PrivateMessageReportAuditLogEntity> reportAuditLogs = [];
    private readonly List<MemberMessageBlockEntity> blocks = [];
    private readonly Func<Guid, MemberAccount?>? resolveMember;
    private long nextSortKey = 1;
    private long nextReportAuditId = 1;

    public InMemoryPrivateMessageRepository(Func<Guid, MemberAccount?>? resolveMember = null)
    {
        this.resolveMember = resolveMember;
    }

    public Task<PrivateInboxPage> GetInboxAsync(
        Guid memberId,
        int page = 1,
        int pageSize = PrivateMessageLimits.InboxPageSize,
        CancellationToken cancellationToken = default) =>
        GetInboxCore(memberId, page, pageSize, isArchived: false);

    public Task<PrivateInboxPage> GetArchivedInboxAsync(
        Guid memberId,
        int page = 1,
        int pageSize = PrivateMessageLimits.InboxPageSize,
        CancellationToken cancellationToken = default) =>
        GetInboxCore(memberId, page, pageSize, isArchived: true);

    public Task<bool> ArchiveConversationAsync(
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

            participant.IsArchived = true;
            return Task.FromResult(true);
        }
    }

    public Task<bool> UnarchiveConversationAsync(
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

            participant.IsArchived = false;
            return Task.FromResult(true);
        }
    }

    private Task<PrivateInboxPage> GetInboxCore(Guid memberId, int page, int pageSize, bool isArchived)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, PrivateMessageLimits.MaxInboxPageSize);

        lock (sync)
        {
            var mine = participants
                .Where(p => p.MemberId == memberId && p.IsArchived == isArchived && !p.IsRemoved)
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

            var reportedIds = reports
                .Where(r => r.ConversationId == conversationId && r.ReporterMemberId == memberId)
                .Select(r => r.MessageId)
                .ToHashSet();
            IReadOnlyList<PrivateMessageItem> items = pageMessages
                .Select(m => new PrivateMessageItem(
                    m.Id,
                    m.SenderMemberId,
                    resolveMember?.Invoke(m.SenderMemberId)?.DisplayName ?? "Unknown member",
                    m.Body,
                    m.CreatedAt,
                    m.SenderMemberId == memberId,
                    m.SortKey,
                    reportedIds.Contains(m.Id)))
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

    public Task<Guid?> GetOtherParticipantIdAsync(
        Guid conversationId,
        Guid memberId,
        CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            var conversation = conversations.SingleOrDefault(c => c.Id == conversationId);
            if (conversation is null
                || (conversation.MemberLowId != memberId && conversation.MemberHighId != memberId))
            {
                return Task.FromResult<Guid?>(null);
            }

            return Task.FromResult<Guid?>(
                conversation.MemberLowId == memberId
                    ? conversation.MemberHighId
                    : conversation.MemberLowId);
        }
    }

    public Task<bool> HasConversationBetweenAsync(
        Guid memberA,
        Guid memberB,
        CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            var (low, high) = OrderPair(memberA, memberB);
            return Task.FromResult(conversations.Any(c => c.MemberLowId == low && c.MemberHighId == high));
        }
    }

    public Task<bool> IsBlockedAsync(
        Guid blockerMemberId,
        Guid blockedMemberId,
        CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            return Task.FromResult(blocks.Any(
                b => b.BlockerMemberId == blockerMemberId && b.BlockedMemberId == blockedMemberId));
        }
    }

    public Task<bool> IsMessagingBlockedAsync(
        Guid memberA,
        Guid memberB,
        CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            return Task.FromResult(blocks.Any(
                b => (b.BlockerMemberId == memberA && b.BlockedMemberId == memberB)
                    || (b.BlockerMemberId == memberB && b.BlockedMemberId == memberA)));
        }
    }

    public Task BlockAsync(
        Guid blockerMemberId,
        Guid blockedMemberId,
        DateTimeOffset blockedAt,
        CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            if (blocks.Any(b => b.BlockerMemberId == blockerMemberId && b.BlockedMemberId == blockedMemberId))
            {
                return Task.CompletedTask;
            }

            blocks.Add(new MemberMessageBlockEntity
            {
                Id = Guid.NewGuid(),
                BlockerMemberId = blockerMemberId,
                BlockedMemberId = blockedMemberId,
                CreatedAt = blockedAt,
            });
            return Task.CompletedTask;
        }
    }

    public Task<bool> UnblockAsync(
        Guid blockerMemberId,
        Guid blockedMemberId,
        CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            var removed = blocks.RemoveAll(
                b => b.BlockerMemberId == blockerMemberId && b.BlockedMemberId == blockedMemberId);
            return Task.FromResult(removed > 0);
        }
    }

    public Task<int> CountMessagesBySenderSinceAsync(
        Guid senderMemberId,
        DateTimeOffset sinceUtc,
        CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            var count = messages.Count(
                m => m.SenderMemberId == senderMemberId && m.CreatedAt >= sinceUtc);
            return Task.FromResult(count);
        }
    }

    public Task<int> CountIdenticalMessagesBySenderSinceAsync(
        Guid senderMemberId,
        string body,
        DateTimeOffset sinceUtc,
        CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            var count = messages.Count(
                m => m.SenderMemberId == senderMemberId
                    && m.CreatedAt >= sinceUtc
                    && string.Equals(m.Body, body, StringComparison.Ordinal));
            return Task.FromResult(count);
        }
    }

    public Task<int> CountDistinctNewRecipientsSinceAsync(
        Guid senderMemberId,
        DateTimeOffset sinceUtc,
        CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            var newConversationIds = conversations
                .Where(c => c.CreatedAt >= sinceUtc)
                .Select(c => c.Id)
                .ToHashSet();

            var count = messages
                .Where(m => m.SenderMemberId == senderMemberId
                    && m.CreatedAt >= sinceUtc
                    && newConversationIds.Contains(m.ConversationId))
                .Select(m => m.ConversationId)
                .Distinct()
                .Count();
            return Task.FromResult(count);
        }
    }

    public Task<PrivateMessageReportResult> CreateReportAsync(
        Guid reporterMemberId,
        Guid conversationId,
        Guid messageId,
        string? reason,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            var message = messages.SingleOrDefault(m => m.Id == messageId);
            if (message is null || message.ConversationId != conversationId)
            {
                return Task.FromResult(new PrivateMessageReportResult(
                    false,
                    null,
                    PrivateMessageReportText.MessageNotFound));
            }

            if (!participants.Any(p => p.ConversationId == conversationId && p.MemberId == reporterMemberId))
            {
                return Task.FromResult(new PrivateMessageReportResult(
                    false,
                    null,
                    PrivateMessageReportText.NotAParticipant));
            }

            if (message.SenderMemberId == reporterMemberId)
            {
                return Task.FromResult(new PrivateMessageReportResult(
                    false,
                    null,
                    PrivateMessageReportText.CannotReportOwn));
            }

            var existing = reports.SingleOrDefault(
                r => r.ReporterMemberId == reporterMemberId && r.MessageId == messageId);
            if (existing is not null)
            {
                return Task.FromResult(new PrivateMessageReportResult(
                    true,
                    existing.Id,
                    null,
                    AlreadyReported: true));
            }

            var senderName = resolveMember?.Invoke(message.SenderMemberId)?.DisplayName ?? "Unknown member";
            var preceding = messages
                .Where(m => m.ConversationId == conversationId && m.SortKey < message.SortKey)
                .OrderByDescending(m => m.SortKey)
                .Take(PrivateMessageLimits.ReportPrecedingMessageCount)
                .OrderBy(m => m.SortKey)
                .Select(m => new PrivateMessageReportContextItem(
                    m.Id,
                    m.SenderMemberId,
                    resolveMember?.Invoke(m.SenderMemberId)?.DisplayName ?? "Unknown member",
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
            reports.Add(entity);
            return Task.FromResult(new PrivateMessageReportResult(true, entity.Id, null));
        }
    }

    public Task<PrivateMessageReport?> GetReportAsync(
        Guid reportId,
        CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            var entity = reports.SingleOrDefault(r => r.Id == reportId);
            return Task.FromResult(entity is null ? null : PrivateMessageReportMapping.ToModel(entity));
        }
    }

    public Task<IReadOnlySet<Guid>> GetReportedMessageIdsAsync(
        Guid conversationId,
        Guid reporterMemberId,
        CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            IReadOnlySet<Guid> ids = reports
                .Where(r => r.ConversationId == conversationId && r.ReporterMemberId == reporterMemberId)
                .Select(r => r.MessageId)
                .ToHashSet();
            return Task.FromResult(ids);
        }
    }

    public Task<PrivateMessageReportListPage> ListReportsAsync(
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var statusFilter = NormalizeOptionalReportStatus(status);

        lock (sync)
        {
            var filtered = reports
                .Where(report => statusFilter is null || report.Status == statusFilter)
                .OrderByDescending(report => report.CreatedAt)
                .ToList();

            IReadOnlyList<PrivateMessageReportListItem> items = filtered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(report => new PrivateMessageReportListItem(
                    report.Id,
                    report.ReporterMemberId,
                    ResolveDisplayName(report.ReporterMemberId),
                    report.ReportedMemberId,
                    ResolveDisplayName(report.ReportedMemberId),
                    report.Reason,
                    report.CreatedAt,
                    report.Status))
                .ToList();

            return Task.FromResult(new PrivateMessageReportListPage(items, filtered.Count, statusFilter));
        }
    }

    public Task<PrivateMessageReportReviewContext?> GetReportedMessageContextAsync(
        Guid reportId,
        CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            var entity = reports.SingleOrDefault(report => report.Id == reportId);
            if (entity is null)
            {
                return Task.FromResult<PrivateMessageReportReviewContext?>(null);
            }

            IReadOnlyList<PrivateMessageReportAuditLog> auditLogs = reportAuditLogs
                .Where(log => log.ReportId == reportId)
                .OrderByDescending(log => log.OccurredAt)
                .ThenByDescending(log => log.Id)
                .Select(log => new PrivateMessageReportAuditLog(
                    log.Id,
                    log.ReportId,
                    log.Action,
                    log.ActorEmail,
                    log.OccurredAt,
                    log.Details))
                .ToList();

            return Task.FromResult<PrivateMessageReportReviewContext?>(
                new PrivateMessageReportReviewContext(
                    PrivateMessageReportMapping.ToModel(entity),
                    ResolveDisplayName(entity.ReporterMemberId),
                    ResolveDisplayName(entity.ReportedMemberId),
                    entity.ReviewerEmail,
                    entity.ReviewedAt,
                    entity.ReviewNotes,
                    auditLogs));
        }
    }

    public Task<PrivateMessageReport?> UpdateReportStatusAsync(
        Guid reportId,
        string status,
        string actorEmail,
        string? reviewNotes,
        CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            var entity = reports.SingleOrDefault(report => report.Id == reportId);
            if (entity is null)
            {
                return Task.FromResult<PrivateMessageReport?>(null);
            }

            var normalized = PrivateMessageReportStatus.Normalize(status);
            var previous = entity.Status;
            entity.Status = normalized;
            entity.ReviewedAt = DateTimeOffset.UtcNow;
            entity.ReviewerEmail = NormalizeOptional(actorEmail, 256);
            entity.ReviewNotes = NormalizeOptional(reviewNotes, PrivateMessageLimits.MaxReportReviewNotesLength);
            AddAuditLocked(
                reportId,
                PrivateMessageReportAuditActions.StatusChanged,
                actorEmail,
                $"{previous} → {normalized}");

            return Task.FromResult<PrivateMessageReport?>(PrivateMessageReportMapping.ToModel(entity));
        }
    }

    public Task<bool> RecordAccessAsync(
        Guid reportId,
        string action,
        string actorEmail,
        string? details,
        CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            if (!reports.Any(report => report.Id == reportId))
            {
                return Task.FromResult(false);
            }

            AddAuditLocked(reportId, action.Trim(), actorEmail, details);
            return Task.FromResult(true);
        }
    }

    public Task<int> CountOpenAsync(CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            return Task.FromResult(reports.Count(report => report.Status == PrivateMessageReportStatus.Open));
        }
    }

    private void AddAuditLocked(Guid reportId, string action, string actorEmail, string? details)
    {
        reportAuditLogs.Add(new PrivateMessageReportAuditLogEntity
        {
            Id = nextReportAuditId++,
            ReportId = reportId,
            Action = action,
            ActorEmail = NormalizeRequired(actorEmail, 256),
            OccurredAt = DateTimeOffset.UtcNow,
            Details = NormalizeOptional(details, 2000),
        });
    }

    private string ResolveDisplayName(Guid memberId) =>
        resolveMember?.Invoke(memberId)?.DisplayName ?? "Unknown member";

    private static string? NormalizeOptionalReportStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status) || string.Equals(status, "all", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return PrivateMessageReportStatus.Normalize(status);
    }

    private static string NormalizeRequired(string value, int maxLength)
    {
        var trimmed = string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
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
