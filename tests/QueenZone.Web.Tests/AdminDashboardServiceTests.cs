using Microsoft.Extensions.DependencyInjection;
using QueenZone.Data;
using QueenZone.Web;
using QueenZone.Web.Pages.Admin;

namespace QueenZone.Web.Tests;

public sealed class AdminDashboardServiceTests
{
    [Fact]
    public async Task GetSnapshotAsync_loads_member_and_submission_tiles()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddQueenZoneInMemoryData();
        services.AddSingleton<IGoogleAnalyticsTrafficService, StubTrafficService>();
        services.AddScoped<AdminDashboardService>();
        await using var provider = services.BuildServiceProvider();

        var dashboard = provider.GetRequiredService<AdminDashboardService>();
        var snapshot = await dashboard.GetSnapshotAsync();

        Assert.NotNull(snapshot.MemberStats);
        Assert.Equal(SubmissionTypeCounts.Empty, snapshot.SubmissionQueue.Photos);
        Assert.Equal(SubmissionTypeCounts.Empty, snapshot.SubmissionQueue.NewsSuggestions);
        Assert.Equal(SubmissionTypeCounts.Empty, snapshot.SubmissionQueue.Articles);
        Assert.Equal("stub-traffic", snapshot.Traffic.UnavailableReason);
    }

    [Fact]
    public void CombineTopContributors_merges_by_member_and_orders_by_count()
    {
        var memberA = Guid.NewGuid();
        var memberB = Guid.NewGuid();
        var photos = new[]
        {
            new SubmissionContributor(memberA, "Alice", 2),
        };
        var news = new[]
        {
            new SubmissionContributor(memberA, "Alice", 3),
            new SubmissionContributor(memberB, "Bob", 4),
        };
        var articles = Array.Empty<SubmissionContributor>();

        var combined = AdminDashboardService.CombineTopContributors(photos, news, articles, maxCount: 5);

        Assert.Equal(2, combined.Count);
        Assert.Equal(memberA, combined[0].MemberId);
        Assert.Equal(5, combined[0].Count);
        Assert.Equal(memberB, combined[1].MemberId);
        Assert.Equal(4, combined[1].Count);
    }

    private sealed class StubTrafficService : IGoogleAnalyticsTrafficService
    {
        public Task<GoogleAnalyticsTrafficSnapshot> GetDashboardTrafficAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(GoogleAnalyticsTrafficSnapshot.Unavailable("stub-traffic"));
    }
}
