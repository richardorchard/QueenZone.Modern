namespace QueenZone.Data;

public sealed record PhotoSubmission(
    Guid Id,
    Guid SubmitterMemberId,
    string Title,
    string? Description,
    string? SuggestedCategory,
    string? ApprovedCategory,
    int? ApproximateYear,
    DateOnly? ApproximateDate,
    string BlobPath,
    string WebOptimizedBlobPath,
    string ThumbnailBlobPath,
    string OriginalFileName,
    long FileSizeBytes,
    string MimeType,
    int? ImageWidthPx,
    int? ImageHeightPx,
    string Status,
    DateTimeOffset SubmittedAt,
    DateTimeOffset? ReviewedAt,
    string? ReviewerEmail,
    string? ReviewNotes,
    string? RejectionReason,
    string? SubmitterDisplayName = null,
    string? SubmitterEmail = null);

public sealed record PhotoSubmissionListItem(
    Guid Id,
    string Title,
    Guid SubmitterMemberId,
    string SubmitterDisplayName,
    DateTimeOffset SubmittedAt,
    string? SuggestedCategory,
    string Status,
    string ThumbnailBlobPath);

public sealed record NewPhotoSubmission(
    Guid SubmitterMemberId,
    string Title,
    string? Description,
    string? SuggestedCategory,
    int? ApproximateYear,
    DateOnly? ApproximateDate,
    string BlobPath,
    string WebOptimizedBlobPath,
    string ThumbnailBlobPath,
    string OriginalFileName,
    long FileSizeBytes,
    string MimeType,
    int? ImageWidthPx,
    int? ImageHeightPx,
    Guid? Id = null);
