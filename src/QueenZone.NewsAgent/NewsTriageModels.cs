using System.Text.Json.Serialization;

using QueenZone.Data;

namespace QueenZone.NewsAgent;

public sealed record NewsTriageStructuredResult(
    [property: JsonPropertyName("verdict")] NewsTriageVerdict Verdict,
    [property: JsonPropertyName("relevance_score")] decimal RelevanceScore,
    [property: JsonPropertyName("confidence_score")] decimal ConfidenceScore,
    [property: JsonPropertyName("rationale")] string Rationale,
    [property: JsonPropertyName("suggested_category")] string? SuggestedCategory,
    [property: JsonPropertyName("entities")] IReadOnlyList<string> Entities,
    [property: JsonPropertyName("review_notes")] string? ReviewNotes);

public sealed record NewsTriageDeterministicSignals(
    bool KeywordMatch,
    bool CanonicalUrlKnown,
    int? DuplicateOfCandidateId,
    string? DuplicateReason);

public sealed record NewsTriageDecision(
    NewsTriageVerdict Verdict,
    NewsCandidateStatus TargetStatus,
    decimal RelevanceScore,
    decimal ConfidenceScore,
    string ReviewNotes,
    int? DuplicateOfCandidateId,
    bool UsedAi,
    bool DeterministicOnly);

public sealed record NewsTriageRunOptions(
    bool DryRun = false,
    int? PerRunCandidateLimit = null,
    DateTime? RunAtUtc = null);

public sealed record NewsTriageRunResult(
    int CandidatesConsidered,
    int PromotedToReview,
    int Rejected,
    int MarkedDuplicate,
    int Skipped,
    int Failures,
    IReadOnlyList<string> Errors);

public sealed record NewsTriageCandidateResult(
    int CandidateId,
    NewsTriageDecision Decision,
    bool Succeeded,
    string? ErrorMessage);
