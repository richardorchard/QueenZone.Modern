using QueenZone.Data;

namespace QueenZone.Web;

/// <summary>
/// Stable list/card shape for public news surfaces (homepage, archive).
/// </summary>
public sealed record NewsArchiveItem(
    int Id,
    string Title,
    string Excerpt,
    DateTime PublishedAt,
    string DetailPath,
    Guid? SubmitterMemberId = null,
    string? SubmitterDisplayName = null,
    string? ImageBlobKey = null,
    int? ImageGalleryPicId = null,
    string? ImageUrl = null,
    string? ThumbnailUrl = null)
{
    /// <summary>
    /// Listing thumbnail: resolved photo URL, or the shared placeholder when unset.
    /// </summary>
    public string DisplayThumbnailUrl => ThumbnailUrl ?? NewsArticleImage.PlaceholderPath;

    /// <summary>
    /// Full-size photo URL, or the shared placeholder when unset.
    /// </summary>
    public string DisplayImageUrl => ImageUrl ?? NewsArticleImage.PlaceholderPath;
}

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
    string DetailPath,
    Guid? SubmitterMemberId = null,
    string? SubmitterDisplayName = null,
    string? ImageBlobKey = null,
    int? ImageGalleryPicId = null,
    string? ImageUrl = null,
    string? ThumbnailUrl = null)
{
    public string DisplayThumbnailUrl => ThumbnailUrl ?? NewsArticleImage.PlaceholderPath;

    public string DisplayImageUrl => ImageUrl ?? NewsArticleImage.PlaceholderPath;
}
