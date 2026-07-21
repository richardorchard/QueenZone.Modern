using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using QueenZone.Data;
using QueenZone.Web;
using QueenZone.Web.Pages.Admin;

namespace QueenZone.Web.Tests;

/// <summary>
/// Tests for content pipeline &amp; AI discovery health tiles on the admin dashboard (issue #292).
/// </summary>
public sealed class AdminDashboardContentPipelineTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string AdminEmail = "admin@test.local";
    private readonly WebApplicationFactory<Program> factory;

    public AdminDashboardContentPipelineTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
    }

    [Fact]
    public async Task CountCandidates_ReturnsNeedsReviewOnly()
    {
        var repo = CreateRepository();
        var sourceId = await SeedSourceAsync(repo);
        var discoveredAt = DateTime.UtcNow;

        await repo.CreateCandidateAsync(Candidate(sourceId, "https://example.com/a", "A", discoveredAt));
        var needsReviewId = await repo.CreateCandidateAsync(
            Candidate(sourceId, "https://example.com/b", "B", discoveredAt));
        await repo.TryUpdateCandidateStatusAsync(
            needsReviewId,
            new NewsCandidateStatusUpdate(NewsCandidateStatus.NeedsReview));

        Assert.Equal(1, await repo.CountCandidatesAsync(NewsCandidateStatus.NeedsReview));
        Assert.Equal(1, await repo.CountCandidatesAsync(NewsCandidateStatus.Discovered));
    }

    [Fact]
    public async Task GetAiPipelineHealth_ReturnsZerosWhenEmpty()
    {
        var repo = CreateRepository();
        var health = await repo.GetAiPipelineHealthAsync(DateTime.UtcNow);

        Assert.Equal(NewsAiPipelineHealth.Empty, health);
        Assert.True(health.IsStale(DateTime.UtcNow));
    }

    [Fact]
    public async Task GetAiPipelineHealth_CountsRunsErrorsAndLastSuccess()
    {
        var repo = CreateRepository();
        var sourceId = await SeedSourceAsync(repo);
        var utcNow = DateTime.UtcNow;
        var candidateId = await repo.CreateCandidateAsync(
            Candidate(sourceId, "https://example.com/pipeline", "Pipeline", utcNow));

        var successRunId = await repo.CreateAiRunAsync(new NewsAiRunCreateRequest(
            candidateId,
            NewsAiRunKind.Triage,
            "openrouter",
            "test-model",
            "v1",
            utcNow.AddHours(-2)));
        await repo.CompleteAiRunAsync(successRunId, new NewsAiRunCompletion(
            NewsAiRunStatus.Succeeded, 10, 5, 0.01m, "{}", null, utcNow.AddHours(-1)));

        var failedRunId = await repo.CreateAiRunAsync(new NewsAiRunCreateRequest(
            candidateId,
            NewsAiRunKind.DraftGeneration,
            "openrouter",
            "test-model",
            "v1",
            utcNow.AddHours(-1)));
        await repo.CompleteAiRunAsync(failedRunId, new NewsAiRunCompletion(
            NewsAiRunStatus.Failed, null, null, null, null, "boom", utcNow.AddMinutes(-30)));

        var health = await repo.GetAiPipelineHealthAsync(utcNow);

        Assert.Equal(2, health.RunsLast24Hours);
        Assert.Equal(1, health.ErrorCountLast24Hours);
        Assert.Equal(utcNow.AddHours(-1), health.LastSuccessfulRunAtUtc);
        Assert.False(health.IsStale(utcNow));
    }

    [Fact]
    public async Task GetAiPipelineHealth_IsStale_WhenLastSuccessOlderThan25Hours()
    {
        var repo = CreateRepository();
        var sourceId = await SeedSourceAsync(repo);
        var utcNow = DateTime.UtcNow;
        var candidateId = await repo.CreateCandidateAsync(
            Candidate(sourceId, "https://example.com/stale", "Stale", utcNow.AddDays(-2)));

        var runId = await repo.CreateAiRunAsync(new NewsAiRunCreateRequest(
            candidateId,
            NewsAiRunKind.Triage,
            "openrouter",
            "test-model",
            "v1",
            utcNow.AddHours(-30)));
        await repo.CompleteAiRunAsync(runId, new NewsAiRunCompletion(
            NewsAiRunStatus.Succeeded, 1, 1, 0.001m, "{}", null, utcNow.AddHours(-26)));

        var health = await repo.GetAiPipelineHealthAsync(utcNow);

        Assert.Equal(0, health.RunsLast24Hours);
        Assert.True(health.IsStale(utcNow));
    }

    [Fact]
    public void MergeRecentlyPublished_OrdersByPublishDateAndCapsAtMax()
    {
        var news = new[]
        {
            new NewsItem(1, "News old", "", "", new DateTime(2026, 7, 1), null, true),
            new NewsItem(2, "News new", "", "", new DateTime(2026, 7, 10), null, true),
        };
        var articles = new[]
        {
            new ArticleItem(3, "Article mid", "", "", new DateTime(2026, 7, 5), null, null, true),
        };

        var merged = IndexModel.MergeRecentlyPublished(news, articles, maxCount: 2);

        Assert.Equal(2, merged.Count);
        Assert.Equal("News new", merged[0].Title);
        Assert.Equal("News", merged[0].Kind);
        Assert.Equal("Article mid", merged[1].Title);
        Assert.Equal("Article", merged[1].Kind);
    }

    [Fact]
    public async Task AdminDashboard_RendersContentPipelineSection()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Email", AdminEmail);

        var response = await client.GetAsync("/admin");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Content pipeline", body);
        Assert.Contains("/admin/news-discovery?status=NeedsReview", body);
        Assert.Contains("AI pipeline", body);
        Assert.Contains("Recently published", body);
    }

    [Fact]
    public async Task AdminDashboard_ShowsAmberWhenAiPipelineIsStale()
    {
        // Sample seed uses a July 2026 success, so Testing env is stale relative to "now".
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Email", AdminEmail);

        var body = await client.GetStringAsync("/admin");

        Assert.Contains("admin-dashboard__queue-tile--amber", body);
        Assert.Contains("No successful run in over 25 hours", body);
    }

    private static InMemoryNewsDiscoveryRepository CreateRepository() =>
        new(new SharedNewsDiscoveryStore());

    private static async Task<int> SeedSourceAsync(INewsDiscoveryRepository repo) =>
        await repo.UpsertSourceAsync(new NewsDiscoverySourceDraft(
            "test-source",
            "Test Source",
            "https://example.com/",
            "https://example.com/feed",
            NewsDiscoverySourceType.Rss,
            NewsDiscoveryTrustTier.Primary,
            60,
            true,
            null));

    private static NewsCandidateCreateRequest Candidate(
        int sourceId,
        string url,
        string title,
        DateTime discoveredAt) =>
        new(sourceId, url, title, discoveredAt, "excerpt", discoveredAt);
}
