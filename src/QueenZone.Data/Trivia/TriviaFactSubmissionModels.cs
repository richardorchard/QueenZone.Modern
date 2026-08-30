namespace QueenZone.Data;

public sealed record TriviaFactSubmission(
    Guid Id,
    Guid SubmitterMemberId,
    string Text,
    string? Category,
    string? Difficulty,
    string? SourceNote,
    string Status,
    DateTimeOffset SubmittedAt,
    DateTimeOffset? ReviewedAt,
    string? ReviewerEmail,
    string? ReviewNotes,
    string? RejectionReason,
    int? PromotedTriviaId = null,
    string? SubmitterDisplayName = null,
    string? SubmitterEmail = null);

public sealed record TriviaFactSubmissionListItem(
    Guid Id,
    string Text,
    string SubmitterDisplayName,
    DateTimeOffset SubmittedAt,
    string? Category,
    string Status);

public sealed record NewTriviaFactSubmission(
    Guid SubmitterMemberId,
    string Text,
    string? Category,
    string? Difficulty,
    string? SourceNote,
    Guid? Id = null);
