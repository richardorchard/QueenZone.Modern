using QueenZone.Data;
using QueenZone.NewsAgent;

namespace QueenZone.Web.Tests;

internal static class NewsDiscoveryTestSeeder
{
    public static async Task<int> SeedDiscoveredCandidateAsync(
        INewsDiscoveryRepository repository,
        string canonicalUrl = "https://www.queenonline.com/news/tour-2026",
        string title = "Queen announce 2026 tour",
        string excerpt = "Official dates announced.",
        DateTime? discoveredAt = null,
        string sourceKey = "queen-online",
        string sourceName = "Queen Online",
        string sourceHomepageUrl = "https://www.queenonline.com/",
        string? sourceFeedOrSiteUrl = "https://www.queenonline.com/feed/",
        NewsDiscoverySourceType sourceType = NewsDiscoverySourceType.Rss,
        NewsDiscoveryTrustTier trustTier = NewsDiscoveryTrustTier.Primary)
    {
        var effectiveDiscoveredAt = discoveredAt ?? DateTime.UtcNow;
        var sourceId = await repository.UpsertSourceAsync(new NewsDiscoverySourceDraft(
            sourceKey,
            sourceName,
            sourceHomepageUrl,
            sourceFeedOrSiteUrl,
            sourceType,
            trustTier,
            60,
            true,
            null));

        return await repository.CreateCandidateAsync(new NewsCandidateCreateRequest(
            sourceId,
            canonicalUrl,
            title,
            effectiveDiscoveredAt,
            excerpt,
            effectiveDiscoveredAt));
    }

    public static async Task<int> SeedNeedsReviewCandidateAsync(
        INewsDiscoveryRepository repository,
        string canonicalUrl = "https://www.queenonline.com/news/tour-2026",
        string title = "Queen announce 2026 tour",
        string excerpt = "Official dates announced.",
        DateTime? discoveredAt = null,
        string sourceKey = "queen-online",
        string sourceName = "Queen Online",
        string sourceHomepageUrl = "https://www.queenonline.com/",
        string? sourceFeedOrSiteUrl = "https://www.queenonline.com/feed/",
        NewsDiscoverySourceType sourceType = NewsDiscoverySourceType.Rss,
        NewsDiscoveryTrustTier trustTier = NewsDiscoveryTrustTier.Primary,
        decimal? relevanceScore = 0.92m,
        decimal? confidenceScore = 0.90m)
    {
        var candidateId = await SeedDiscoveredCandidateAsync(
            repository,
            canonicalUrl,
            title,
            excerpt,
            discoveredAt,
            sourceKey,
            sourceName,
            sourceHomepageUrl,
            sourceFeedOrSiteUrl,
            sourceType,
            trustTier);

        await repository.TryUpdateCandidateStatusAsync(
            candidateId,
            new NewsCandidateStatusUpdate(
                NewsCandidateStatus.NeedsReview,
                RelevanceScore: relevanceScore,
                ConfidenceScore: confidenceScore));

        return candidateId;
    }

    public static async Task<int> SeedDraftedCandidateAsync(
        INewsDiscoveryRepository repository,
        string canonicalUrl = "https://www.queenonline.com/news/review-candidate",
        string title = "Discovery review candidate",
        string excerpt = "Official dates announced for 2026.",
        DateTime? discoveredAt = null)
    {
        var effectiveDiscoveredAt = discoveredAt ?? new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
        var candidateId = await SeedNeedsReviewCandidateAsync(
            repository,
            canonicalUrl,
            title,
            excerpt,
            effectiveDiscoveredAt,
            sourceFeedOrSiteUrl: null,
            relevanceScore: 0.9m,
            confidenceScore: 0.88m);

        await repository.TryUpdateCandidateStatusAsync(
            candidateId,
            new NewsCandidateStatusUpdate(NewsCandidateStatus.Drafted));

        var aiRunId = await repository.CreateAiRunAsync(new NewsAiRunCreateRequest(
            candidateId,
            NewsAiRunKind.Triage,
            "openrouter",
            "openai/gpt-4.1-nano",
            "triage-v1",
            effectiveDiscoveredAt));

        await repository.CompleteAiRunAsync(aiRunId, new NewsAiRunCompletion(
            NewsAiRunStatus.Succeeded,
            100,
            50,
            0.0001m,
            """{"verdict":"relevant","rationale":"Mentions Queen tour dates.","entities":["Queen"]}""",
            null,
            effectiveDiscoveredAt));

        await repository.UpsertDraftAsync(candidateId, new NewsAgentDraftUpsert(
            "Discovery draft title",
            "discovery-draft-title",
            "Draft excerpt for review queue.",
            "Draft body for review queue.",
            "Source: Queen Online",
            "Official announcement.",
            "High confidence.",
            effectiveDiscoveredAt.Date,
            null));

        return candidateId;
    }

    public static async Task<int> SeedNeedsReviewCandidateWithDraftAsync(
        INewsDiscoveryRepository repository,
        string canonicalUrl = "https://www.queenonline.com/news/needs-review-draft",
        string title = "Needs review with draft",
        string excerpt = "Excerpt",
        DateTime? discoveredAt = null)
    {
        var effectiveDiscoveredAt = discoveredAt ?? new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
        var candidateId = await SeedNeedsReviewCandidateAsync(
            repository,
            canonicalUrl,
            title,
            excerpt,
            effectiveDiscoveredAt,
            sourceFeedOrSiteUrl: null,
            relevanceScore: null,
            confidenceScore: null);

        await repository.UpsertDraftAsync(candidateId, new NewsAgentDraftUpsert(
            "Needs-review draft title",
            "needs-review-draft-title",
            "Needs-review excerpt.",
            "Needs-review body.",
            "Source: Queen Online",
            null,
            null,
            effectiveDiscoveredAt.Date,
            null));

        return candidateId;
    }

    public static async Task<(int FirstId, int SecondId)> SeedDuplicatePairAsync(
        INewsDiscoveryRepository repository,
        DateTime? discoveredAt = null)
    {
        var effectiveDiscoveredAt = discoveredAt ?? new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
        var firstId = await SeedNeedsReviewCandidateAsync(
            repository,
            canonicalUrl: "https://www.queenonline.com/news/original",
            title: "Original Queen story",
            excerpt: "Excerpt",
            discoveredAt: effectiveDiscoveredAt,
            sourceFeedOrSiteUrl: null,
            relevanceScore: null,
            confidenceScore: null);

        var secondId = await SeedNeedsReviewCandidateAsync(
            repository,
            canonicalUrl: "https://www.queenonline.com/news/duplicate-story",
            title: "Original Queen story",
            excerpt: "Duplicate excerpt.",
            discoveredAt: effectiveDiscoveredAt.AddMinutes(5),
            sourceFeedOrSiteUrl: null,
            relevanceScore: null,
            confidenceScore: null);

        return (firstId, secondId);
    }

    public static async Task<int> SeedDiscoveredCandidateWithDraftAsync(INewsDiscoveryRepository repository)
    {
        var discoveredAt = new DateTime(2026, 7, 3, 12, 0, 0, DateTimeKind.Utc);
        var candidateId = await SeedDiscoveredCandidateAsync(
            repository,
            canonicalUrl: "https://example.com/discovered-with-draft",
            title: "Discovered with draft",
            excerpt: "Excerpt",
            discoveredAt: discoveredAt,
            sourceKey: "discovered-draft-source",
            sourceName: "Discovered draft source",
            sourceHomepageUrl: "https://example.com/",
            sourceFeedOrSiteUrl: null,
            sourceType: NewsDiscoverySourceType.AllowlistedPage,
            trustTier: NewsDiscoveryTrustTier.Primary);

        await repository.UpsertDraftAsync(
            candidateId,
            new NewsAgentDraftUpsert(
                "Undrafted promote title",
                "undrafted-promote-title",
                "Excerpt",
                "Body",
                null,
                null,
                null,
                discoveredAt.Date,
                null));

        return candidateId;
    }
}
