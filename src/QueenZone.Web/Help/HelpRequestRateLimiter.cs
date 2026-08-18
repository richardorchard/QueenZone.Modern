using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace QueenZone.Web;

public sealed class HelpRequestRateLimiter(
    IMemoryCache cache,
    TimeProvider timeProvider,
    IOptions<HelpRequestOptions> options)
{
    public bool IsAllowed(string? clientIp)
    {
        if (string.IsNullOrWhiteSpace(clientIp))
        {
            return false;
        }

        var limit = Math.Max(1, options.Value.MaxAnonymousPerIpPerHour);
        var now = timeProvider.GetUtcNow();
        var key = $"help-request-ip:{clientIp.Trim()}:{now.ToUnixTimeSeconds() / 3600}";
        var count = cache.GetOrCreate(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            return 0;
        });

        if (count >= limit)
        {
            return false;
        }

        cache.Set(key, count + 1, TimeSpan.FromHours(1));
        return true;
    }
}
