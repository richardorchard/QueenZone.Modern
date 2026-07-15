using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using QueenZone.Data;

namespace QueenZone.Web;

public sealed class ForumPostRateLimiter(
    IForumWriteRepository forumWriteRepository,
    IMemoryCache cache,
    TimeProvider timeProvider,
    ILogger<ForumPostRateLimiter> logger)
{
    public const int MaxPostsPerMinute = 5;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    public async Task<bool> IsAllowedAsync(Guid memberId, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var key = $"forum-post-rate:{memberId:N}:{now.ToUnixTimeSeconds() / 60}";
        if (cache.TryGetValue<int>(key, out var cachedCount) && cachedCount >= MaxPostsPerMinute)
        {
            return false;
        }

        int count;
        try
        {
            count = await forumWriteRepository.CountPostsByMemberSinceAsync(memberId, now - Window, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException && !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                ex,
                "Forum post rate-limit probe failed for member {MemberId}; allowing this post attempt.",
                memberId);
            cache.Set(key, 1, Window);
            return true;
        }

        if (count >= MaxPostsPerMinute)
        {
            cache.Set(key, count, Window);
            return false;
        }

        cache.Set(key, count + 1, Window);
        return true;
    }
}
