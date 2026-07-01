namespace QueenZone.Data;

public sealed record NewsCandidateListQuery(
    NewsCandidateStatus? Status = null,
    int? SourceId = null,
    NewsDiscoveryTrustTier? TrustTier = null,
    decimal? MinConfidence = null,
    string? Entity = null,
    DateTime? DiscoveredFromUtc = null,
    DateTime? DiscoveredToUtc = null,
    bool? HasDraft = null);
