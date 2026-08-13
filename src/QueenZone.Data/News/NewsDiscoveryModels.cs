namespace QueenZone.Data;

public sealed record NewsDiscoverySource(
    int Id,
    string Key,
    string DisplayName,
    string HomepageUrl,
    string? FeedOrSiteUrl,
    NewsDiscoverySourceType SourceType,
    NewsDiscoveryTrustTier TrustTier,
    int PollIntervalMinutes,
    bool Enabled,
    string? RelevanceKeywords,
    DateTime? LastFetchedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record NewsDiscoverySourceDraft(
    string Key,
    string DisplayName,
    string HomepageUrl,
    string? FeedOrSiteUrl,
    NewsDiscoverySourceType SourceType,
    NewsDiscoveryTrustTier TrustTier,
    int PollIntervalMinutes,
    bool Enabled,
    string? RelevanceKeywords);

public sealed record NewsCandidate(
    int Id,
    int SourceId,
    string SourceUrl,
    string CanonicalUrl,
    string CanonicalUrlHash,
    string SourceTitle,
    DateTime? SourcePublishedAt,
    DateTime DiscoveredAt,
    string? ContentHash,
    NewsCandidateStatus Status,
    decimal? RelevanceScore,
    decimal? ConfidenceScore,
    int? DuplicateOfCandidateId,
    int? PromotedNewsId,
    string? ReviewNotes,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string SourceKey,
    string SourceDisplayName,
    NewsDiscoveryTrustTier SourceTrustTier);

public sealed record NewsCandidateCreateRequest(
    int SourceId,
    string SourceUrl,
    string SourceTitle,
    DateTime? SourcePublishedAt,
    string? Excerpt,
    DateTime DiscoveredAt);

public sealed record NewsCandidateEvidence(
    int Id,
    int CandidateId,
    string SourceUrl,
    string CanonicalUrl,
    string SourceName,
    NewsDiscoveryTrustTier SourceTrustTier,
    string FetchedTitle,
    DateTime? FetchedPublishedAt,
    string? Excerpt,
    string? ContentHash,
    DateTime FetchedAt,
    string? Etag,
    DateTime CreatedAt);

public sealed record NewsCandidateEvidenceDraft(
    string SourceUrl,
    string SourceName,
    NewsDiscoveryTrustTier SourceTrustTier,
    string FetchedTitle,
    DateTime? FetchedPublishedAt,
    string? Excerpt,
    string? Etag,
    DateTime FetchedAt);

public sealed record NewsAiRun(
    int Id,
    int CandidateId,
    NewsAiRunKind Kind,
    string ModelProvider,
    string ModelId,
    string PromptVersion,
    NewsAiRunStatus Status,
    int? InputTokens,
    int? OutputTokens,
    decimal? EstimatedCostUsd,
    string? StructuredResultJson,
    string? ErrorMessage,
    DateTime StartedAt,
    DateTime? CompletedAt,
    DateTime CreatedAt);

public sealed record NewsAiRunCreateRequest(
    int CandidateId,
    NewsAiRunKind Kind,
    string ModelProvider,
    string ModelId,
    string PromptVersion,
    DateTime StartedAt);

public sealed record NewsAiRunCompletion(
    NewsAiRunStatus Status,
    int? InputTokens,
    int? OutputTokens,
    decimal? EstimatedCostUsd,
    string? StructuredResultJson,
    string? ErrorMessage,
    DateTime CompletedAt);

public sealed record NewsAgentDraft(
    int Id,
    int CandidateId,
    int? AiRunId,
    string ProposedTitle,
    string? ProposedSlug,
    string ProposedExcerpt,
    string ProposedBody,
    string? AttributionText,
    string? SourceNotes,
    string? ConfidenceNotes,
    DateTime? SuggestedPublishAt,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record NewsAgentDraftUpsert(
    string ProposedTitle,
    string? ProposedSlug,
    string ProposedExcerpt,
    string ProposedBody,
    string? AttributionText,
    string? SourceNotes,
    string? ConfidenceNotes,
    DateTime? SuggestedPublishAt,
    int? AiRunId);

public sealed record NewsCandidateStatusUpdate(
    NewsCandidateStatus Status,
    string? ReviewNotes = null,
    decimal? RelevanceScore = null,
    decimal? ConfidenceScore = null,
    int? DuplicateOfCandidateId = null,
    int? PromotedNewsId = null);
