using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class PrivateMessageRateLimiterTests
{
    private static readonly Guid SenderId = Guid.NewGuid();
    private static readonly DateTime EstablishedAccountCreatedAt = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task IsSendAllowedAsync_Denies_WhenMessageVolumeExceedsLimit()
    {
        var repository = new StubPrivateMessageRepository { MessageCount = 20 };
        var limiter = CreateLimiter(repository, new PrivateMessageRateLimitOptions { MaxMessagesPerWindow = 20 });

        var allowed = await limiter.IsSendAllowedAsync(
            SenderId,
            EstablishedAccountCreatedAt,
            "Hello",
            isNewConversation: false);

        Assert.False(allowed);
    }

    [Fact]
    public async Task IsSendAllowedAsync_Denies_WhenDuplicateBodySentTooOften()
    {
        var repository = new StubPrivateMessageRepository { IdenticalMessageCount = 3 };
        var limiter = CreateLimiter(
            repository,
            new PrivateMessageRateLimitOptions { MaxDuplicateMessagesPerWindow = 3 });

        var allowed = await limiter.IsSendAllowedAsync(
            SenderId,
            EstablishedAccountCreatedAt,
            "Same message",
            isNewConversation: false);

        Assert.False(allowed);
    }

    [Fact]
    public async Task IsSendAllowedAsync_Denies_WhenNewConversationRecipientFanOutExceedsLimit()
    {
        var repository = new StubPrivateMessageRepository { DistinctNewRecipientCount = 8 };
        var limiter = CreateLimiter(
            repository,
            new PrivateMessageRateLimitOptions { MaxNewRecipientsPerWindow = 8 });

        var allowed = await limiter.IsSendAllowedAsync(
            SenderId,
            EstablishedAccountCreatedAt,
            "Hello",
            isNewConversation: true);

        Assert.False(allowed);
    }

    [Fact]
    public async Task IsSendAllowedAsync_IgnoresRecipientFanOut_WhenReplyingNotComposing()
    {
        var repository = new StubPrivateMessageRepository { DistinctNewRecipientCount = 999 };
        var limiter = CreateLimiter(
            repository,
            new PrivateMessageRateLimitOptions { MaxNewRecipientsPerWindow = 8 });

        var allowed = await limiter.IsSendAllowedAsync(
            SenderId,
            EstablishedAccountCreatedAt,
            "Hello",
            isNewConversation: false);

        Assert.True(allowed);
    }

    [Fact]
    public async Task IsSendAllowedAsync_UsesStricterLimits_ForNewAccounts()
    {
        var repository = new StubPrivateMessageRepository { MessageCount = 5 };
        var options = new PrivateMessageRateLimitOptions
        {
            NewAccountAgeDays = 3,
            NewAccountMaxMessagesPerWindow = 5,
            MaxMessagesPerWindow = 20,
        };
        var limiter = CreateLimiter(repository, options);
        var recentlyCreated = DateTime.UtcNow.AddDays(-1);

        var allowed = await limiter.IsSendAllowedAsync(
            SenderId,
            recentlyCreated,
            "Hello",
            isNewConversation: false);

        // Denied under the stricter new-account limit even though the standard limit (20) is not hit.
        Assert.False(allowed);
    }

    [Fact]
    public async Task IsSendAllowedAsync_Allows_WhenUnderAllLimits()
    {
        var repository = new StubPrivateMessageRepository
        {
            MessageCount = 1,
            IdenticalMessageCount = 0,
            DistinctNewRecipientCount = 1,
        };
        var limiter = CreateLimiter(repository, new PrivateMessageRateLimitOptions());

        var allowed = await limiter.IsSendAllowedAsync(
            SenderId,
            EstablishedAccountCreatedAt,
            "Hello",
            isNewConversation: true);

        Assert.True(allowed);
    }

    [Fact]
    public async Task IsSendAllowedAsync_DeniesSend_WhenRateLimitProbeFails()
    {
        var limiter = CreateLimiter(
            new ThrowingPrivateMessageRepository(),
            new PrivateMessageRateLimitOptions());

        var allowed = await limiter.IsSendAllowedAsync(
            SenderId,
            EstablishedAccountCreatedAt,
            "Hello",
            isNewConversation: true);

        // Fail-closed: database outage must not open a spam window.
        Assert.False(allowed);
    }

    private static PrivateMessageRateLimiter CreateLimiter(
        IPrivateMessageRepository repository,
        PrivateMessageRateLimitOptions options) =>
        new(
            repository,
            TimeProvider.System,
            Options.Create(options),
            NullLogger<PrivateMessageRateLimiter>.Instance);

    private sealed class StubPrivateMessageRepository : NotImplementedPrivateMessageRepository
    {
        public int MessageCount { get; set; }

        public int IdenticalMessageCount { get; set; }

        public int DistinctNewRecipientCount { get; set; }

        public override Task<int> CountMessagesBySenderSinceAsync(
            Guid senderMemberId,
            DateTimeOffset sinceUtc,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(MessageCount);

        public override Task<int> CountIdenticalMessagesBySenderSinceAsync(
            Guid senderMemberId,
            string body,
            DateTimeOffset sinceUtc,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(IdenticalMessageCount);

        public override Task<int> CountDistinctNewRecipientsSinceAsync(
            Guid senderMemberId,
            DateTimeOffset sinceUtc,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(DistinctNewRecipientCount);
    }

    private sealed class ThrowingPrivateMessageRepository : NotImplementedPrivateMessageRepository
    {
        public override Task<int> CountMessagesBySenderSinceAsync(
            Guid senderMemberId,
            DateTimeOffset sinceUtc,
            CancellationToken cancellationToken = default) =>
            throw new TimeoutException("Rate-limit probe timed out.");
    }

    /// <summary>
    /// Base fake that throws for every member not overridden, so each test-specific fake above
    /// only needs to implement the methods <see cref="PrivateMessageRateLimiter"/> actually calls.
    /// </summary>
    private abstract class NotImplementedPrivateMessageRepository : IPrivateMessageRepository
    {
        public virtual Task<PrivateInboxPage> GetInboxAsync(
            Guid memberId,
            int page = 1,
            int pageSize = PrivateMessageLimits.InboxPageSize,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public virtual Task<int> CountUnreadConversationsAsync(
            Guid memberId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public virtual Task<PrivateConversationDetail?> GetConversationAsync(
            Guid conversationId,
            Guid memberId,
            int? page = null,
            int pageSize = PrivateMessageLimits.ConversationPageSize,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public virtual Task<bool> IsParticipantAsync(
            Guid conversationId,
            Guid memberId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public virtual Task MarkConversationReadAsync(
            Guid conversationId,
            Guid memberId,
            long lastReadSortKey,
            DateTimeOffset readAt,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public virtual Task<PrivateMessageSendResult> SendNewOrExistingAsync(
            Guid senderMemberId,
            Guid recipientMemberId,
            string body,
            DateTimeOffset sentAt,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public virtual Task<PrivateMessageSendResult> ReplyAsync(
            Guid conversationId,
            Guid senderMemberId,
            string body,
            DateTimeOffset sentAt,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public virtual Task<PrivateInboxPage> GetArchivedInboxAsync(
            Guid memberId,
            int page = 1,
            int pageSize = PrivateMessageLimits.InboxPageSize,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public virtual Task<bool> ArchiveConversationAsync(
            Guid conversationId,
            Guid memberId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public virtual Task<bool> UnarchiveConversationAsync(
            Guid conversationId,
            Guid memberId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public virtual Task<bool> RemoveConversationAsync(
            Guid conversationId,
            Guid memberId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public virtual Task<Guid?> GetOtherParticipantIdAsync(
            Guid conversationId,
            Guid memberId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public virtual Task<bool> HasConversationBetweenAsync(
            Guid memberA,
            Guid memberB,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public virtual Task<bool> IsBlockedAsync(
            Guid blockerMemberId,
            Guid blockedMemberId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public virtual Task<bool> IsMessagingBlockedAsync(
            Guid memberA,
            Guid memberB,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public virtual Task BlockAsync(
            Guid blockerMemberId,
            Guid blockedMemberId,
            DateTimeOffset blockedAt,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public virtual Task<bool> UnblockAsync(
            Guid blockerMemberId,
            Guid blockedMemberId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public virtual Task<int> CountMessagesBySenderSinceAsync(
            Guid senderMemberId,
            DateTimeOffset sinceUtc,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public virtual Task<int> CountIdenticalMessagesBySenderSinceAsync(
            Guid senderMemberId,
            string body,
            DateTimeOffset sinceUtc,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public virtual Task<int> CountDistinctNewRecipientsSinceAsync(
            Guid senderMemberId,
            DateTimeOffset sinceUtc,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public virtual Task<PrivateMessageReportResult> CreateReportAsync(
            Guid reporterMemberId,
            Guid conversationId,
            Guid messageId,
            string? reason,
            DateTimeOffset createdAt,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public virtual Task<PrivateMessageReport?> GetReportAsync(
            Guid reportId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public virtual Task<IReadOnlySet<Guid>> GetReportedMessageIdsAsync(
            Guid conversationId,
            Guid reporterMemberId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public virtual Task<PrivateMessageReportListPage> ListReportsAsync(
            string? status,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public virtual Task<int> CountOpenReportsAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public virtual Task<PrivateMessageReport?> UpdateReportStatusAsync(
            Guid reportId,
            string status,
            string actorEmail,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public virtual Task AppendReportViewedAuditAsync(
            Guid reportId,
            string actorEmail,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public virtual Task<int> PurgeExpiredReportsAsync(
            DateTimeOffset asOfUtc,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
