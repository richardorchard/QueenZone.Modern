using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class EfNewsDiscoveryRepositoryTests : IAsyncDisposable
{
    private readonly SqliteConnection connection;
    private readonly QueenZoneDbContext dbContext;
    private readonly EfNewsDiscoveryRepository repository;

    public EfNewsDiscoveryRepositoryTests()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<QueenZoneDbContext>()
            .UseSqlite(connection)
            .Options;
        dbContext = new QueenZoneDbContext(options);
        dbContext.Database.EnsureCreated();
        repository = new EfNewsDiscoveryRepository(dbContext);
    }

    [Fact]
    public async Task Source_lifecycle_supports_lookup_update_and_fetch_timestamp()
    {
        var sourceId = await repository.UpsertSourceAsync(new NewsDiscoverySourceDraft(
            "queen-online",
            "Queen Online",
            "https://www.queenonline.com/",
            "https://www.queenonline.com/feed",
            NewsDiscoverySourceType.Rss,
            NewsDiscoveryTrustTier.Primary,
            60,
            true,
            "queen,tour"));

        var fetchedAt = new DateTime(2026, 7, 1, 8, 0, 0, DateTimeKind.Utc);
        await repository.MarkSourceFetchedAsync(sourceId, fetchedAt);

        var source = await repository.GetSourceByKeyAsync("queen-online");
        Assert.NotNull(source);
        Assert.Equal(fetchedAt, source.LastFetchedAt);

        await repository.UpsertSourceAsync(new NewsDiscoverySourceDraft(
            "queen-online",
            "Queen Official",
            "https://www.queenonline.com/",
            "https://www.queenonline.com/feed",
            NewsDiscoverySourceType.Rss,
            NewsDiscoveryTrustTier.Primary,
            90,
            false,
            "queen"));

        var sources = await repository.GetSourcesAsync(enabledOnly: true);
        Assert.Empty(sources);

        var allSources = await repository.GetSourcesAsync();
        Assert.Single(allSources);
        Assert.Equal("Queen Official", allSources[0].DisplayName);
        Assert.False(allSources[0].Enabled);
    }

    [Fact]
    public async Task CreateCandidate_persists_provenance_and_blocks_duplicate_urls()
    {
        var sourceId = await repository.UpsertSourceAsync(new NewsDiscoverySourceDraft(
            "brian-may",
            "Brian May",
            "https://brianmay.com/",
            null,
            NewsDiscoverySourceType.AllowlistedPage,
            NewsDiscoveryTrustTier.Primary,
            120,
            true,
            null));

        var discoveredAt = new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
        var candidateId = await repository.CreateCandidateAsync(new NewsCandidateCreateRequest(
            sourceId,
            "https://brianmay.com/blog/story?utm_source=newsletter",
            "Studio update",
            discoveredAt,
            "Brief update.",
            discoveredAt));

        var candidate = await repository.GetCandidateByIdAsync(candidateId);
        Assert.NotNull(candidate);
        Assert.Equal("https://brianmay.com/blog/story", candidate.CanonicalUrl);

        var duplicateLookup = await repository.GetCandidateByCanonicalUrlHashAsync(candidate!.CanonicalUrlHash);
        Assert.NotNull(duplicateLookup);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.CreateCandidateAsync(new NewsCandidateCreateRequest(
                sourceId,
                "https://brianmay.com/blog/story?fbclid=abc",
                "Duplicate",
                discoveredAt,
                "Duplicate.",
                discoveredAt)));
    }

    [Fact]
    public async Task Candidate_queries_filter_by_status_and_source()
    {
        var primarySourceId = await repository.UpsertSourceAsync(new NewsDiscoverySourceDraft(
            "queen-online",
            "Queen Online",
            "https://www.queenonline.com/",
            null,
            NewsDiscoverySourceType.Rss,
            NewsDiscoveryTrustTier.Primary,
            60,
            true,
            null));
        var secondarySourceId = await repository.UpsertSourceAsync(new NewsDiscoverySourceDraft(
            "nme",
            "NME",
            "https://www.nme.com/music",
            null,
            NewsDiscoverySourceType.Rss,
            NewsDiscoveryTrustTier.Secondary,
            60,
            true,
            null));

        var discoveredAt = DateTime.UtcNow;
        var primaryCandidateId = await repository.CreateCandidateAsync(new NewsCandidateCreateRequest(
            primarySourceId,
            "https://www.queenonline.com/news/tour",
            "Tour announced",
            discoveredAt,
            "Tour dates.",
            discoveredAt));
        await repository.CreateCandidateAsync(new NewsCandidateCreateRequest(
            secondarySourceId,
            "https://www.nme.com/music/queen-tour",
            "Queen tour coverage",
            discoveredAt,
            "Press coverage.",
            discoveredAt));

        await repository.TryUpdateCandidateStatusAsync(
            primaryCandidateId,
            new NewsCandidateStatusUpdate(NewsCandidateStatus.NeedsReview));

        var needsReview = await repository.GetCandidatesAsync(NewsCandidateStatus.NeedsReview);
        Assert.Single(needsReview);

        var primaryCandidates = await repository.GetCandidatesAsync(sourceId: primarySourceId);
        Assert.Single(primaryCandidates);
    }

    [Fact]
    public async Task TryUpdateCandidateStatus_returns_false_for_missing_or_invalid_transition()
    {
        Assert.False(await repository.TryUpdateCandidateStatusAsync(
            999,
            new NewsCandidateStatusUpdate(NewsCandidateStatus.Rejected)));

        var sourceId = await repository.UpsertSourceAsync(new NewsDiscoverySourceDraft(
            "queen-online",
            "Queen Online",
            "https://www.queenonline.com/",
            null,
            NewsDiscoverySourceType.Rss,
            NewsDiscoveryTrustTier.Primary,
            60,
            true,
            null));
        var discoveredAt = DateTime.UtcNow;
        var candidateId = await repository.CreateCandidateAsync(new NewsCandidateCreateRequest(
            sourceId,
            "https://www.queenonline.com/news/box-set",
            "Box set",
            discoveredAt,
            "Collector edition.",
            discoveredAt));

        Assert.False(await repository.TryUpdateCandidateStatusAsync(
            candidateId,
            new NewsCandidateStatusUpdate(NewsCandidateStatus.PromotedToArticle)));
    }

    [Fact]
    public async Task Evidence_ai_run_and_draft_metadata_round_trip()
    {
        var sourceId = await repository.UpsertSourceAsync(new NewsDiscoverySourceDraft(
            "queen-online",
            "Queen Online",
            "https://www.queenonline.com/",
            null,
            NewsDiscoverySourceType.Rss,
            NewsDiscoveryTrustTier.Primary,
            60,
            true,
            null));
        var discoveredAt = DateTime.UtcNow;
        var candidateId = await repository.CreateCandidateAsync(new NewsCandidateCreateRequest(
            sourceId,
            "https://www.queenonline.com/news/documentary",
            "New documentary",
            discoveredAt,
            "Official announcement.",
            discoveredAt));

        var evidenceId = await repository.AddCandidateEvidenceAsync(candidateId, new NewsCandidateEvidenceDraft(
            "https://www.queenonline.com/news/documentary",
            "Queen Online",
            NewsDiscoveryTrustTier.Primary,
            "New documentary",
            discoveredAt,
            "Updated excerpt.",
            "\"etag-1\"",
            discoveredAt.AddMinutes(5)));
        var evidence = await repository.GetCandidateEvidenceAsync(candidateId);
        Assert.Equal(2, evidence.Count);
        Assert.Contains(evidence, item => item.Id == evidenceId && item.Excerpt == "Updated excerpt.");

        var startedAt = DateTime.UtcNow;
        var aiRunId = await repository.CreateAiRunAsync(new NewsAiRunCreateRequest(
            candidateId,
            NewsAiRunKind.Triage,
            "openrouter",
            "openai/gpt-4.1-nano",
            "triage-v1",
            startedAt));
        await repository.CompleteAiRunAsync(aiRunId, new NewsAiRunCompletion(
            NewsAiRunStatus.Failed,
            10,
            0,
            null,
            null,
            "Provider timeout",
            startedAt.AddSeconds(30)));

        var draftId = await repository.UpsertDraftAsync(candidateId, new NewsAgentDraftUpsert(
            "Draft title",
            "draft-title",
            "Draft excerpt",
            "Draft body",
            "Source: Queen Online",
            "Official source.",
            "Needs editor review.",
            discoveredAt.AddDays(1),
            aiRunId));
        var updatedDraftId = await repository.UpsertDraftAsync(candidateId, new NewsAgentDraftUpsert(
            "Updated draft title",
            "updated-draft-title",
            "Updated excerpt",
            "Updated body",
            "Source: Queen Online",
            "Official source.",
            "Still needs review.",
            discoveredAt.AddDays(2),
            aiRunId));

        var draft = await repository.GetDraftByCandidateIdAsync(candidateId);
        var runs = await repository.GetAiRunsForCandidateAsync(candidateId);
        Assert.Equal(draftId, updatedDraftId);
        Assert.NotNull(draft);
        Assert.Equal("Updated draft title", draft.ProposedTitle);
        Assert.Single(runs);
        Assert.Equal(NewsAiRunStatus.Failed, runs[0].Status);
        Assert.Equal("Provider timeout", runs[0].ErrorMessage);
    }

    [Fact]
    public async Task MarkSourceFetched_throws_when_source_is_missing() =>
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.MarkSourceFetchedAsync(999, DateTime.UtcNow));

    [Fact]
    public async Task CompleteAiRun_throws_when_run_is_missing() =>
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.CompleteAiRunAsync(
                999,
                new NewsAiRunCompletion(
                    NewsAiRunStatus.Failed,
                    null,
                    null,
                    null,
                    null,
                    "Missing",
                    DateTime.UtcNow)));

    [Fact]
    public async Task FindEarlierDuplicateCandidateAsync_returns_earlier_title_match()
    {
        var sourceId = await repository.UpsertSourceAsync(new NewsDiscoverySourceDraft(
            "queen-online",
            "Queen Online",
            "https://www.queenonline.com/",
            "https://www.queenonline.com/feed/",
            NewsDiscoverySourceType.Rss,
            NewsDiscoveryTrustTier.Primary,
            60,
            true,
            null));
        var firstId = await repository.CreateCandidateAsync(new NewsCandidateCreateRequest(
            sourceId,
            "https://www.queenonline.com/news/tour-2026",
            "Queen announce 2026 tour",
            DateTime.UtcNow,
            "Official dates announced.",
            DateTime.UtcNow));
        var secondId = await repository.CreateCandidateAsync(new NewsCandidateCreateRequest(
            sourceId,
            "https://www.queenonline.com/news/tour-2026-copy",
            "Queen announce 2026 tour",
            DateTime.UtcNow,
            "Different excerpt.",
            DateTime.UtcNow));

        var duplicate = await repository.FindEarlierDuplicateCandidateAsync(
            secondId,
            "Queen announce 2026 tour",
            null);

        Assert.NotNull(duplicate);
        Assert.Equal(firstId, duplicate.Id);
    }

    public async ValueTask DisposeAsync()
    {
        await dbContext.DisposeAsync();
        await connection.DisposeAsync();
    }
}
