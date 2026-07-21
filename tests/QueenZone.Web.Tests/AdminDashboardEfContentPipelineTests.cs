using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QueenZone.Data;
using QueenZone.Data.Entities;

namespace QueenZone.Web.Tests;

/// <summary>
/// EF / SQLite-backed tests for content pipeline dashboard methods (issue #292).
/// </summary>
public sealed class AdminDashboardEfContentPipelineTests : IAsyncLifetime
{
    private readonly SqliteConnection connection;
    private QueenZoneDbContext dbContext = null!;

    public AdminDashboardEfContentPipelineTests()
    {
        connection = new SqliteConnection("Data Source=:memory:");
    }

    public async Task InitializeAsync()
    {
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<QueenZoneDbContext>()
            .UseSqlite(connection)
            .Options;
        dbContext = new QueenZoneDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await dbContext.DisposeAsync();
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task CountCandidates_ReturnsZerosWhenEmpty()
    {
        var repo = new EfNewsDiscoveryRepository(dbContext);
        Assert.Equal(0, await repo.CountCandidatesAsync(NewsCandidateStatus.NeedsReview));
    }

    [Fact]
    public async Task CountCandidates_FiltersByStatus()
    {
        var source = await SeedSourceAsync();
        dbContext.NewsCandidates.AddRange(
            Candidate(source.Id, NewsCandidateStatus.Discovered, "https://example.com/d"),
            Candidate(source.Id, NewsCandidateStatus.NeedsReview, "https://example.com/n1"),
            Candidate(source.Id, NewsCandidateStatus.NeedsReview, "https://example.com/n2"));
        await dbContext.SaveChangesAsync();

        var repo = new EfNewsDiscoveryRepository(dbContext);
        Assert.Equal(2, await repo.CountCandidatesAsync(NewsCandidateStatus.NeedsReview));
        Assert.Equal(1, await repo.CountCandidatesAsync(NewsCandidateStatus.Discovered));
    }

    [Fact]
    public async Task GetAiPipelineHealth_AggregatesRunsAndErrors()
    {
        var source = await SeedSourceAsync();
        var candidate = Candidate(source.Id, NewsCandidateStatus.NeedsReview, "https://example.com/ai");
        dbContext.NewsCandidates.Add(candidate);
        await dbContext.SaveChangesAsync();

        var utcNow = DateTime.UtcNow;
        dbContext.NewsAiRuns.AddRange(
            AiRun(candidate.Id, NewsAiRunStatus.Succeeded, utcNow.AddHours(-3), utcNow.AddHours(-2)),
            AiRun(candidate.Id, NewsAiRunStatus.Failed, utcNow.AddHours(-1), utcNow.AddMinutes(-30)),
            AiRun(candidate.Id, NewsAiRunStatus.Succeeded, utcNow.AddHours(-40), utcNow.AddHours(-39)));
        await dbContext.SaveChangesAsync();

        var repo = new EfNewsDiscoveryRepository(dbContext);
        var health = await repo.GetAiPipelineHealthAsync(utcNow);

        Assert.Equal(2, health.RunsLast24Hours);
        Assert.Equal(1, health.ErrorCountLast24Hours);
        Assert.Equal(utcNow.AddHours(-2), health.LastSuccessfulRunAtUtc);
        Assert.False(health.IsStale(utcNow));
    }

    [Fact]
    public async Task GetAiPipelineHealth_IsStaleWithoutRecentSuccess()
    {
        var repo = new EfNewsDiscoveryRepository(dbContext);
        var health = await repo.GetAiPipelineHealthAsync(DateTime.UtcNow);

        Assert.Equal(NewsAiPipelineHealth.Empty, health);
        Assert.True(health.IsStale(DateTime.UtcNow));
    }

    private async Task<NewsDiscoverySourceEntity> SeedSourceAsync()
    {
        var source = new NewsDiscoverySourceEntity
        {
            Key = $"src-{Guid.NewGuid():N}",
            DisplayName = "EF Test Source",
            HomepageUrl = "https://example.com/",
            FeedOrSiteUrl = "https://example.com/feed",
            SourceType = NewsDiscoverySourceType.Rss,
            TrustTier = NewsDiscoveryTrustTier.Primary,
            PollIntervalMinutes = 60,
            Enabled = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        dbContext.NewsDiscoverySources.Add(source);
        await dbContext.SaveChangesAsync();
        return source;
    }

    private static NewsCandidateEntity Candidate(int sourceId, NewsCandidateStatus status, string url) =>
        new()
        {
            SourceId = sourceId,
            SourceUrl = url,
            CanonicalUrl = url,
            CanonicalUrlHash = NewsCandidateDedupe.ComputeUrlHash(url),
            SourceTitle = "Title",
            Status = status,
            DiscoveredAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

    private static NewsAiRunEntity AiRun(
        int candidateId,
        NewsAiRunStatus status,
        DateTime startedAt,
        DateTime completedAt) =>
        new()
        {
            CandidateId = candidateId,
            Kind = NewsAiRunKind.Triage,
            ModelProvider = "openrouter",
            ModelId = "test",
            PromptVersion = "v1",
            Status = status,
            StartedAt = startedAt,
            CompletedAt = completedAt,
            CreatedAt = startedAt,
            ErrorMessage = status == NewsAiRunStatus.Failed ? "fail" : null,
        };
}
