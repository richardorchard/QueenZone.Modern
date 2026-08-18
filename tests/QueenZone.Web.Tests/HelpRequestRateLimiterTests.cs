using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class HelpRequestRateLimiterTests
{
    [Fact]
    public void IsAllowed_DeniesMissingIp()
    {
        var limiter = CreateLimiter();

        Assert.False(limiter.IsAllowed(null));
        Assert.False(limiter.IsAllowed(" "));
    }

    [Fact]
    public void IsAllowed_AllowsUpToConfiguredLimitThenDenies()
    {
        var limiter = CreateLimiter(maxPerHour: 2);

        Assert.True(limiter.IsAllowed("203.0.113.10"));
        Assert.True(limiter.IsAllowed("203.0.113.10"));
        Assert.False(limiter.IsAllowed("203.0.113.10"));
        Assert.True(limiter.IsAllowed("203.0.113.11"));
    }

    private static HelpRequestRateLimiter CreateLimiter(int maxPerHour = 3)
    {
        return new HelpRequestRateLimiter(
            new MemoryCache(new MemoryCacheOptions()),
            TimeProvider.System,
            Options.Create(new HelpRequestOptions { MaxAnonymousPerIpPerHour = maxPerHour }));
    }
}
