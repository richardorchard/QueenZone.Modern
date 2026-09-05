namespace QueenZone.Web;

/// <summary>
/// Shared status fields for <c>/api/v1/me/submissions/*</c>.
/// <see cref="Status"/> is the existing workflow value (for example
/// <c>Pending</c>, <c>UnderReview</c>, <c>Approved</c>), not a mobile-only enum.
/// <see cref="StatusLabel"/> and <see cref="StatusTone"/> match the website badge.
/// </summary>
public sealed record SubmissionStatusDto(string Status, string StatusLabel, string StatusTone);

public sealed record PhotoSubmissionItemDto(
    Guid Id,
    string Title,
    DateTimeOffset SubmittedAt,
    SubmissionStatusDto Status,
    string? Notes,
    string? ThumbnailPath,
    int? PromotedPicId);

public sealed record NewsSuggestionItemDto(
    Guid Id,
    string Url,
    string TruncatedUrl,
    string? Title,
    DateTimeOffset SubmittedAt,
    SubmissionStatusDto Status,
    string? Notes,
    int? PublishedNewsId,
    string? PublishedPath);

public sealed record FanPerformanceSubmissionItemDto(
    Guid Id,
    string Title,
    string CoveredSong,
    string PerformedBy,
    DateTimeOffset SubmittedAt,
    SubmissionStatusDto Status,
    string? Notes,
    string? RejectionReason,
    int? PromotedStageId,
    string? PublishedPath);

public sealed record ArticleSubmissionItemDto(
    Guid Id,
    string Title,
    DateTimeOffset? SubmittedAt,
    SubmissionStatusDto Status,
    string? Notes,
    bool CanContinueEditing,
    string? EditPath,
    string? PublishedPath);
