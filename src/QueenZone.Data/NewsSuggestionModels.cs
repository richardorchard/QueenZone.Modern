namespace QueenZone.Data;

public sealed record NewsSuggestion(
    Guid Id,
    Guid SubmitterMemberId,
    string Url,
    string UrlHash,
    string? Title,
    string? Notes,
    string Status,
    DateTimeOffset SubmittedAt,
    DateTimeOffset? ReviewedAt,
    string? ReviewerEmail,
    string? ReviewNotes,
    int? PromotedNewsId,
    int? DuplicateCandidateId,
    string? SubmitterDisplayName,
    string? SubmitterEmail);

public sealed record NewsSuggestionListItem(
    Guid Id,
    string Url,
    string? Title,
    string SubmitterDisplayName,
    DateTimeOffset SubmittedAt,
    string Status);
