using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class NewsDiscoveryRepositoryTests
{
    private static InMemoryNewsDiscoveryRepository CreateRepository()
    {
        var store = new SharedNewsDiscoveryStore();
        return new InMemoryNewsDiscoveryRepository(store);
    }

    [Fact]
    public async Task CreateCandidate_persists_provenance_and_dedupe_hash()
    {
        var repository = CreateRepository();
        var sourceId = await repository.UpsertSourceAsync(new NewsDiscoverySourceDraft(
            "queen-online",
            "Queen Online",
            "https://www.queenonline.com/",
            "https://www.queenonline.com/feed",
            NewsDiscoverySourceType.Rss,
            NewsDiscoveryTrustTier.Primary,
            60,
            true,
            "queen,tour,release"));

        var discoveredAt = new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
        var candidateId = await repository.CreateCandidateAsync(new NewsCandidateCreateRequest(
            sourceId,
            "https://www.queenonline.com/news/story?utm_source=twitter",
            "Queen announce new tour",
            discoveredAt,
            "Official dates announced for 2026.",
            discoveredAt));

        var candidate = await repository.GetCandidateByIdAsync(candidateId);
        Assert.NotNull(candidate);
        Assert.Equal(NewsCandidateStatus.Discovered, candidate.Status);
        Assert.Equal("https://www.queenonline.com/news/story", candidate.CanonicalUrl);
        Assert.Equal("queen-online", candidate.SourceKey);
        Assert.Equal(NewsDiscoveryTrustTier.Primary, candidate.SourceTrustTier);

        var evidence = await repository.GetCandidateEvidenceAsync(candidateId);
        Assert.Single(evidence);
        Assert.Equal("Queen Online", evidence[0].SourceName);
        Assert.Equal("Official dates announced for 2026.", evidence[0].Excerpt);

        var duplicateLookup = await repository.GetCandidateByCanonicalUrlHashAsync(candidate.CanonicalUrlHash);
        Assert.NotNull(duplicateLookup);
        Assert.Equal(candidateId, duplicateLookup.Id);
    }

    [Fact]
    public async Task TryUpdateCandidateStatus_rejects_invalid_transition()
    {
        var repository = CreateRepository();
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
        var discoveredAt = DateTime.UtcNow;
        var candidateId = await repository.CreateCandidateAsync(new NewsCandidateCreateRequest(
            sourceId,
            "https://brianmay.com/blog/new-post",
            "Studio update",
            discoveredAt,
            "Brief update.",
            discoveredAt));

        Assert.False(await repository.TryUpdateCandidateStatusAsync(
            candidateId,
            new NewsCandidateStatusUpdate(NewsCandidateStatus.PromotedToArticle)));

        Assert.True(await repository.TryUpdateCandidateStatusAsync(
            candidateId,
            new NewsCandidateStatusUpdate(NewsCandidateStatus.NeedsReview)));
        Assert.True(await repository.TryUpdateCandidateStatusAsync(
            candidateId,
            new NewsCandidateStatusUpdate(NewsCandidateStatus.Drafted, ConfidenceScore: 0.91m)));
        Assert.True(await repository.TryUpdateCandidateStatusAsync(
            candidateId,
            new NewsCandidateStatusUpdate(
                NewsCandidateStatus.PromotedToArticle,
                PromotedNewsId: 42)));

        var candidate = await repository.GetCandidateByIdAsync(candidateId);
        Assert.NotNull(candidate);
        Assert.Equal(NewsCandidateStatus.PromotedToArticle, candidate.Status);
        Assert.Equal(42, candidate.PromotedNewsId);
    }

    [Fact]
    public async Task UpsertDraft_and_ai_run_store_generation_metadata()
    {
        var repository = CreateRepository();
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
            "New box set",
            discoveredAt,
            "Collector edition announced.",
            discoveredAt));
        await repository.TryUpdateCandidateStatusAsync(
            candidateId,
            new NewsCandidateStatusUpdate(NewsCandidateStatus.NeedsReview));
        await repository.TryUpdateCandidateStatusAsync(
            candidateId,
            new NewsCandidateStatusUpdate(NewsCandidateStatus.Drafted));

        var startedAt = DateTime.UtcNow;
        var aiRunId = await repository.CreateAiRunAsync(new NewsAiRunCreateRequest(
            candidateId,
            NewsAiRunKind.DraftGeneration,
            "openrouter",
            "openai/gpt-4.1-mini",
            "draft-v1",
            startedAt));
        await repository.CompleteAiRunAsync(aiRunId, new NewsAiRunCompletion(
            NewsAiRunStatus.Succeeded,
            120,
            240,
            0.0025m,
            """{"title":"New Queen box set announced"}""",
            null,
            startedAt.AddSeconds(2)));

        var draftId = await repository.UpsertDraftAsync(candidateId, new NewsAgentDraftUpsert(
            "New Queen box set announced",
            "new-queen-box-set-announced",
            "Collector edition announced for later this year.",
            "Queen have announced a new collector box set.",
            "Source: Queen Online",
            "Official announcement.",
            "High confidence from primary source.",
            discoveredAt.AddDays(1),
            aiRunId));

        var draft = await repository.GetDraftByCandidateIdAsync(candidateId);
        var runs = await repository.GetAiRunsForCandidateAsync(candidateId);
        Assert.NotNull(draft);
        Assert.Equal(draftId, draft.Id);
        Assert.Equal(aiRunId, draft.AiRunId);
        Assert.Single(runs);
        Assert.Equal(NewsAiRunStatus.Succeeded, runs[0].Status);
        Assert.Equal(0.0025m, runs[0].EstimatedCostUsd);
    }
}
