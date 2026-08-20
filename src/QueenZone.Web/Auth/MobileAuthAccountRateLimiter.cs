using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace QueenZone.Web;

/// <summary>
/// Process-local per-member cap for mobile sign-in completion and refresh-token grants.
/// Complements the IP policy on <c>/api/v1/auth</c>; does not log tokens or secrets.
/// </summary>
public sealed class MobileAuthAccountRateLimiter(
    IMemoryCache cache,
    TimeProvider timeProvider,
    IOptions<AuthRateLimitingOptions> options,
    ILogger<MobileAuthAccountRateLimiter> logger)
{
    public const string ClientMessage = "Too many attempts. Try again later.";

    public bool IsAllowed(Guid memberId)
    {
        var opts = options.Value;
        var limit = Math.Max(1, opts.AccountPermitLimit);
        var windowMinutes = Math.Max(1, opts.AccountWindowMinutes);
        var now = timeProvider.GetUtcNow();
        var bucket = now.ToUnixTimeSeconds() / (windowMinutes * 60L);
        var key = $"mobile-auth-account:{memberId:N}:{bucket}";
        var count = cache.GetOrCreate(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(windowMinutes);
            return 0;
        });

        if (count >= limit)
        {
            logger.LogWarning(
                "Mobile auth rate limit exceeded for member {MemberId} (account partition).",
                memberId);
            return false;
        }

        cache.Set(key, count + 1, TimeSpan.FromMinutes(windowMinutes));
        return true;
    }
}
