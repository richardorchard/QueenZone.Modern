namespace QueenZone.Web;

/// <summary>
/// Multipart fields for <c>POST /api/v1/member/photo-submissions</c>.
/// Same names as <c>/submit/photo</c>; the photo part is <c>photo</c> or <c>PhotoFile</c>.
/// </summary>
public sealed record PhotoSubmissionRequestDto
{
    public string? Title { get; init; }

    public string? Description { get; init; }

    public string? SuggestedCategory { get; init; }

    public int? ApproximateYear { get; init; }

    public DateOnly? ApproximateDate { get; init; }
}

/// <summary>
/// Result of <c>POST /api/v1/member/photo-submissions</c>.
/// <see cref="Status"/> is <c>Pending</c> so a later list/status API can reuse this shape.
/// </summary>
public sealed record PhotoSubmissionCreatedDto(
    Guid Id,
    string Status,
    string Title,
    DateTimeOffset SubmittedAt);

/// <summary>
/// JSON body for <c>POST /api/v1/member/news-suggestions</c>.
/// Identity is the mobile JWT, not a body member id.
/// </summary>
public sealed record NewsSuggestionRequestDto
{
    public string? Url { get; init; }

    public string? Title { get; init; }

    public string? Notes { get; init; }
}

/// <summary>
/// Result of <c>POST /api/v1/member/news-suggestions</c>.
/// <see cref="Url"/> is the normalized canonical URL stored for dedupe.
/// </summary>
public sealed record NewsSuggestionCreatedDto(
    Guid Id,
    string Status,
    string Url,
    string? Title,
    DateTimeOffset SubmittedAt);

/// <summary>
/// Multipart fields for <c>POST /api/v1/member/fan-performance-submissions</c>.
/// Same names as <c>/submit/fan-performance</c>.
/// </summary>
public sealed record FanPerformanceSubmissionRequestDto
{
    public string? Title { get; init; }

    public string? CoveredSong { get; init; }

    public string? PerformedBy { get; init; }

    public string? Description { get; init; }

    public bool RightsDeclarationAccepted { get; init; }
}

/// <summary>
/// Result of <c>POST /api/v1/member/fan-performance-submissions</c>.
/// </summary>
public sealed record FanPerformanceSubmissionCreatedDto(
    Guid Id,
    string Status,
    string Title,
    DateTimeOffset SubmittedAt);
