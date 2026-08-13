namespace QueenZone.Data;

public sealed record NewsCandidateReviewListItem(
    int Id,
    string SourceTitle,
    string SourceUrl,
    string SourceDisplayName,
    NewsDiscoveryTrustTier SourceTrustTier,
    NewsCandidateStatus Status,
    decimal? RelevanceScore,
    decimal? ConfidenceScore,
    DateTime DiscoveredAt,
    int? DuplicateOfCandidateId,
    string? DraftTitle);
