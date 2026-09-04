namespace QueenZone.Data;

public static class FanPerformanceSubmissionRights
{
    public const string DeclarationVersion = "2026-09-v1";

    public static bool IsRecorded(DateTimeOffset declaredAt, string? version) =>
        declaredAt != default && !string.IsNullOrWhiteSpace(version);
}

public sealed record FanPerformanceSubmission(
    Guid Id,
    Guid SubmitterMemberId,
    string Title,
    string CoveredSong,
    string PerformedBy,
    string? Description,
    string BlobPath,
    string OriginalFileName,
    long FileSizeBytes,
    string MimeType,
    int? DurationSeconds,
    string Status,
    DateTimeOffset SubmittedAt,
    DateTimeOffset? ReviewedAt,
    string? ReviewerEmail,
    string? ReviewNotes,
    string? RejectionReason,
    DateTimeOffset RightsDeclaredAt,
    string RightsDeclarationVersion,
    int? PromotedStageId = null,
    string? SubmitterDisplayName = null,
    string? SubmitterEmail = null);

public sealed record NewFanPerformanceSubmission(
    Guid SubmitterMemberId,
    string Title,
    string CoveredSong,
    string PerformedBy,
    string? Description,
    string BlobPath,
    string OriginalFileName,
    long FileSizeBytes,
    string MimeType,
    int? DurationSeconds,
    DateTimeOffset RightsDeclaredAt,
    string RightsDeclarationVersion,
    Guid? Id = null);

public sealed record FanPerformanceSubmissionListItem(
    Guid Id,
    string Title,
    string CoveredSong,
    string PerformedBy,
    Guid SubmitterMemberId,
    string SubmitterDisplayName,
    DateTimeOffset SubmittedAt,
    int? DurationSeconds,
    long FileSizeBytes,
    string Status);

public sealed record FanPerformanceSubmissionAuditEntry(
    long Id,
    string Action,
    string ActorEmail,
    DateTimeOffset OccurredAt,
    string? Details);

public sealed record FanPerformanceReviewEdits(
    string? Title,
    string? PerformedBy,
    string? Description,
    string? CoveredSong);
