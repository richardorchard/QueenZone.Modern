using QueenZone.Data;

namespace QueenZone.NewsAgent;

public sealed record NewsDraftStructuredResult(
    string Title,
    string? Slug,
    string Excerpt,
    string Body,
    IReadOnlyList<string> RelatedEntities,
    IReadOnlyList<string> SourceUrls,
    IReadOnlyList<string> SourceNames,
    string? AttributionText,
    string? ConfidenceNotes,
    string? SourceNotes,
    DateTime? SuggestedPublishAt,
    bool SecondarySourceWarning);

public sealed record NewsDraftSourceAttribution(
    IReadOnlyList<string> SourceUrls,
    IReadOnlyList<string> SourceNames,
    string AttributionText,
    string SourceNotes,
    string ConfidenceNotes);

public sealed record NewsDraftRunOptions(
    bool DryRun = false,
    bool ForceRegenerate = false,
    int? PerRunCandidateLimit = null);

public sealed record NewsDraftRunResult(
    int CandidatesConsidered,
    int DraftsCreated,
    int Skipped,
    int Failures,
    IReadOnlyList<string> Errors);

public sealed record NewsDraftCandidateResult(
    int CandidateId,
    int? DraftId,
    bool Succeeded,
    string? ErrorMessage);
