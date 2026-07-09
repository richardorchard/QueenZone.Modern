namespace QueenZone.Web;

/// <summary>
/// Stable category card/header for public photography surfaces.
/// </summary>
public sealed record PhotoCategorySummary(
    int CatId,
    string Name,
    string Slug,
    int ImageCount,
    string DetailPath,
    string? CoverImageUrl = null);

/// <summary>
/// Stable thumbnail row for category grids.
/// </summary>
public sealed record PhotoThumbnailItem(
    int PicId,
    string Title,
    string ThumbnailUrl,
    int ThumbWidth,
    int ThumbHeight,
    int Year,
    string DetailPath);

/// <summary>
/// Stable detail shape for the photography lightbox page.
/// </summary>
public sealed record PhotoDetailItem(
    int PicId,
    string Title,
    string ImageUrl,
    int Year,
    string DetailPath);
