using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class ForumPostRateLimiterTests
{
    [Fact]
    public async Task IsAllowedAsync_AllowsAttempt_WhenRateLimitProbeFails()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var limiter = new ForumPostRateLimiter(
            new ThrowingForumWriteRepository(),
            cache,
            TimeProvider.System,
            NullLogger<ForumPostRateLimiter>.Instance);

        var allowed = await limiter.IsAllowedAsync(Guid.NewGuid());

        Assert.True(allowed);
    }

    private sealed class ThrowingForumWriteRepository : IForumWriteRepository
    {
        public Task<ForumThreadCreateResult> CreateThreadAsync(
            NewForumThread thread,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<int> CreatePostAsync(
            NewForumPost post,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<ForumEditablePost?> GetPostAsync(
            int postId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<ForumPostUpdateResult> UpdatePostAsync(
            int postId,
            Guid editorMemberId,
            string sanitisedBody,
            bool isAdmin,
            int editWindowMinutes,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<ForumWriteThread?> GetThreadAsync(
            int topicId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<int> CountPostsByMemberSinceAsync(
            Guid memberId,
            DateTimeOffset since,
            CancellationToken cancellationToken = default) =>
            throw new TimeoutException("Rate-limit probe timed out.");

        public Task<int> CountApprovedPostsByMemberAsync(
            Guid memberId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
