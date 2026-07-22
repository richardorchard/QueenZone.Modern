using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class GoogleAnalyticsTrafficServiceTests
{
    [Fact]
    public async Task GetDashboardTrafficAsync_ReturnsUnavailable_WhenPropertyIdMissing()
    {
        var client = new FakeGoogleAnalyticsDataClient(GoogleAnalyticsTrafficSnapshot.Unavailable("Should not call"));
        var service = CreateService(client, new AnalyticsOptions
        {
            GoogleAnalyticsServiceAccountJson = "{}",
        });

        var snapshot = await service.GetDashboardTrafficAsync();

        Assert.False(snapshot.IsAvailable);
        Assert.Contains("property ID", snapshot.UnavailableReason);
        Assert.Equal(0, client.CallCount);
    }

    [Fact]
    public async Task GetDashboardTrafficAsync_ReturnsUnavailable_WhenServiceAccountMissing()
    {
        var client = new FakeGoogleAnalyticsDataClient(GoogleAnalyticsTrafficSnapshot.Unavailable("Should not call"));
        var service = CreateService(client, new AnalyticsOptions
        {
            GoogleAnalyticsPropertyId = "123456",
        });

        var snapshot = await service.GetDashboardTrafficAsync();

        Assert.False(snapshot.IsAvailable);
        Assert.Contains("service account", snapshot.UnavailableReason);
        Assert.Equal(0, client.CallCount);
    }

    [Fact]
    public async Task GetDashboardTrafficAsync_CachesConfiguredResponses()
    {
        var expected = new GoogleAnalyticsTrafficSnapshot(
            true,
            SessionsLast7Days: 120,
            PageViewsLast7Days: 240,
            ActiveUsersLast7Days: 80,
            TopPagesThisWeek: [new GoogleAnalyticsTopPage("/news", 90)],
            DailySessionsLast30Days: [new GoogleAnalyticsDailySession(new DateOnly(2026, 7, 21), 12)]);

        var client = new FakeGoogleAnalyticsDataClient(expected);
        var service = CreateService(client, new AnalyticsOptions
        {
            GoogleAnalyticsPropertyId = "123456",
            GoogleAnalyticsServiceAccountJson = "{}",
            TrafficCacheMinutes = 1,
        });

        var first = await service.GetDashboardTrafficAsync();
        var second = await service.GetDashboardTrafficAsync();

        Assert.Same(expected, first);
        Assert.Same(expected, second);
        Assert.Equal(1, client.CallCount);
    }

    [Fact]
    public async Task GetDashboardTrafficAsync_CachesUnavailableApiFailures()
    {
        var client = new ThrowingGoogleAnalyticsDataClient();
        var service = CreateService(client, new AnalyticsOptions
        {
            GoogleAnalyticsPropertyId = "123456",
            GoogleAnalyticsServiceAccountJson = "{}",
        });

        var first = await service.GetDashboardTrafficAsync();
        var second = await service.GetDashboardTrafficAsync();

        Assert.False(first.IsAvailable);
        Assert.False(second.IsAvailable);
        Assert.Equal(1, client.CallCount);
    }

    private static GoogleAnalyticsTrafficService CreateService(
        IGoogleAnalyticsDataClient client,
        AnalyticsOptions options) =>
        new(
            client,
            new MemoryCache(new MemoryCacheOptions()),
            Options.Create(options),
            NullLogger<GoogleAnalyticsTrafficService>.Instance);

    private sealed class FakeGoogleAnalyticsDataClient(GoogleAnalyticsTrafficSnapshot snapshot) : IGoogleAnalyticsDataClient
    {
        public int CallCount { get; private set; }

        public Task<GoogleAnalyticsTrafficSnapshot> GetDashboardTrafficAsync(
            string propertyId,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(snapshot);
        }
    }

    private sealed class ThrowingGoogleAnalyticsDataClient : IGoogleAnalyticsDataClient
    {
        public int CallCount { get; private set; }

        public Task<GoogleAnalyticsTrafficSnapshot> GetDashboardTrafficAsync(
            string propertyId,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            throw new InvalidOperationException("GA is down");
        }
    }
}

