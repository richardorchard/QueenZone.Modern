namespace QueenZone.Web;

/// <summary>
/// Stable list/card shape for public news surfaces (homepage, archive).
/// </summary>
public sealed record NewsArchiveItem(
    int Id,
    string Title,
    string Excerpt,
    DateTime PublishedAt,
    string DetailPath);

/// <summary>
/// Stable detail shape for public (and admin preview) news article pages.
/// </summary>
public sealed record NewsDetailItem(
    int Id,
    string Title,
    string Excerpt,
    string Body,
    DateTime PublishedAt,
    string? SourceUrl,
    string DetailPath);
