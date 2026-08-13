namespace QueenZone.Data;

public sealed record NewsDiscoveryProvenance(
    int CandidateId,
    string SourceTitle,
    string SourceUrl,
    string SourceDisplayName,
    NewsDiscoveryTrustTier SourceTrustTier,
    decimal? RelevanceScore,
    decimal? ConfidenceScore,
    string? TriageRationale,
    string? DraftModelId,
    DateTime DiscoveredAtUtc,
    DateTime? SuggestedPublishAt);
