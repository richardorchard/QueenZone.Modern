using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace QueenZone.Web;

internal sealed class GoogleAnalyticsTrafficService(
    IGoogleAnalyticsDataClient dataClient,
    IMemoryCache cache,
    IOptions<AnalyticsOptions> options,
    ILogger<GoogleAnalyticsTrafficService> logger) : IGoogleAnalyticsTrafficService
{
    private const string CacheKey = "admin-dashboard:google-analytics-traffic";
    private const int MinimumCacheMinutes = 60;

    public async Task<GoogleAnalyticsTrafficSnapshot> GetDashboardTrafficAsync(
        CancellationToken cancellationToken = default)
    {
        var analyticsOptions = options.Value;
        if (string.IsNullOrWhiteSpace(analyticsOptions.GoogleAnalyticsPropertyId))
        {
            return GoogleAnalyticsTrafficSnapshot.Unavailable("Google Analytics property ID is not configured.");
        }

        if (string.IsNullOrWhiteSpace(analyticsOptions.GoogleAnalyticsServiceAccountJson))
        {
            return GoogleAnalyticsTrafficSnapshot.Unavailable("Google Analytics service account key is not configured.");
        }

        if (cache.TryGetValue(CacheKey, out GoogleAnalyticsTrafficSnapshot? cached) && cached is not null)
        {
            return cached;
        }

        try
        {
            var snapshot = await dataClient.GetDashboardTrafficAsync(
                analyticsOptions.GoogleAnalyticsPropertyId.Trim(),
                cancellationToken);

            cache.Set(CacheKey, snapshot, GetCacheDuration(analyticsOptions));
            return snapshot;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Google Analytics traffic data is unavailable.");
            var unavailable = GoogleAnalyticsTrafficSnapshot.Unavailable("Google Analytics traffic is unavailable.");
            cache.Set(CacheKey, unavailable, GetCacheDuration(analyticsOptions));
            return unavailable;
        }
    }

    private static TimeSpan GetCacheDuration(AnalyticsOptions options) =>
        TimeSpan.FromMinutes(Math.Max(MinimumCacheMinutes, options.TrafficCacheMinutes));
}

