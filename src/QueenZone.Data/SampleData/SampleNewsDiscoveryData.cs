namespace QueenZone.Data;

/// <summary>
/// Seeds a drafted discovery candidate for in-memory runs so local/Testing apps
/// can exercise the admin promote → publish editorial path without the news agent worker.
/// </summary>
public static class SampleNewsDiscoveryData
{
    public const string DraftedCandidateSourceTitle = "E2E editorial workflow source item";
    public const string DraftedCandidateProposedTitle = "E2E editorial workflow draft";
    public const string DraftedCandidateCanonicalUrl =
        "https://www.queenonline.com/news/e2e-editorial-workflow";

    public static void Seed(SharedNewsDiscoveryStore store)
    {
        var discoveredAt = new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
        var sourceId = store.UpsertSource(new NewsDiscoverySourceDraft(
            "queen-online-e2e",
            "Queen Online",
            "https://www.queenonline.com/",
            "https://www.queenonline.com/feed/",
            NewsDiscoverySourceType.Rss,
            NewsDiscoveryTrustTier.Primary,
            60,
            true,
            null));

        var candidateId = store.CreateCandidate(new NewsCandidateCreateRequest(
            sourceId,
            DraftedCandidateCanonicalUrl,
            DraftedCandidateSourceTitle,
            discoveredAt,
            "Official dates announced for the editorial workflow smoke path.",
            discoveredAt));

        store.TryUpdateCandidateStatus(
            candidateId,
            new NewsCandidateStatusUpdate(
                NewsCandidateStatus.NeedsReview,
                RelevanceScore: 0.92m,
                ConfidenceScore: 0.90m));

        store.TryUpdateCandidateStatus(
            candidateId,
            new NewsCandidateStatusUpdate(NewsCandidateStatus.Drafted));

        var aiRunId = store.CreateAiRun(new NewsAiRunCreateRequest(
            candidateId,
            NewsAiRunKind.DraftGeneration,
            "openrouter",
            "openai/gpt-4.1-mini",
            "draft-v1",
            discoveredAt));

        store.CompleteAiRun(aiRunId, new NewsAiRunCompletion(
            NewsAiRunStatus.Succeeded,
            100,
            50,
            0.0001m,
            """{"title":"E2E editorial workflow draft"}""",
            null,
            discoveredAt));

        store.UpsertDraft(candidateId, new NewsAgentDraftUpsert(
            DraftedCandidateProposedTitle,
            "e2e-editorial-workflow-draft",
            "Playwright editorial workflow excerpt.",
            "Playwright editorial workflow body for promote and publish coverage.",
            "Source: Queen Online",
            "Seeded for in-memory editorial workflow tests.",
            "High confidence seed draft.",
            new DateTime(2026, 7, 9, 0, 0, 0, DateTimeKind.Utc),
            aiRunId));
    }
}
