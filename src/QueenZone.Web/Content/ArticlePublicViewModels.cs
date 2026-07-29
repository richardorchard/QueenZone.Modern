namespace QueenZone.Web;

/// <summary>
/// Stable list/card shape for public article archive surfaces.
/// </summary>
public sealed record ArticleArchiveItem(
    int Id,
    string Title,
    string Excerpt,
    DateTime PublishedAt,
    string? CategoryName,
    string DetailPath);

/// <summary>
/// Stable detail shape for public article pages.
/// </summary>
public sealed record ArticleDetailItem(
    int Id,
    string Title,
    string Excerpt,
    string Body,
    DateTime PublishedAt,
    string? Source,
    string? CategoryName,
    string DetailPath);
