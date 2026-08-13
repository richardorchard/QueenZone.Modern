namespace QueenZone.Data;

public static class NewsDiscoveryProvenanceBuilder
{
    public static NewsDiscoveryProvenance Build(
        NewsCandidate candidate,
        NewsAgentDraft? draft,
        IReadOnlyList<NewsAiRun> aiRuns)
    {
        var latestTriage = aiRuns
            .Where(run => run.Kind == NewsAiRunKind.Triage && !string.IsNullOrWhiteSpace(run.StructuredResultJson))
            .OrderByDescending(run => run.StartedAt)
            .FirstOrDefault();

        string? triageRationale = null;
        if (latestTriage?.StructuredResultJson is not null
            && NewsAiStructuredDisplay.TryReadTriageSummary(latestTriage.StructuredResultJson, out var summary)
            && summary is not null)
        {
            triageRationale = summary.Rationale;
        }

        var latestDraftRun = aiRuns
            .Where(run => run.Kind == NewsAiRunKind.DraftGeneration)
            .OrderByDescending(run => run.StartedAt)
            .FirstOrDefault();

        return new NewsDiscoveryProvenance(
            candidate.Id,
            candidate.SourceTitle,
            candidate.SourceUrl,
            candidate.SourceDisplayName,
            candidate.SourceTrustTier,
            candidate.RelevanceScore,
            candidate.ConfidenceScore,
            triageRationale,
            latestDraftRun?.ModelId,
            candidate.DiscoveredAt,
            draft?.SuggestedPublishAt);
    }

    public static async Task<NewsDiscoveryProvenance?> LoadForPromotedArticleAsync(
        INewsDiscoveryRepository repository,
        int promotedNewsId,
        CancellationToken cancellationToken = default)
    {
        var candidate = await repository.GetCandidateByPromotedNewsIdAsync(promotedNewsId, cancellationToken);
        if (candidate is null)
        {
            return null;
        }

        var draft = await repository.GetDraftByCandidateIdAsync(candidate.Id, cancellationToken);
        var aiRuns = await repository.GetAiRunsForCandidateAsync(candidate.Id, cancellationToken);
        return Build(candidate, draft, aiRuns);
    }
}
