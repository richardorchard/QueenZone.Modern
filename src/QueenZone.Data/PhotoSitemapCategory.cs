namespace QueenZone.Data;

/// <summary>
/// Lightweight photography projection for sitemap generation (no image URLs or dimensions).
/// </summary>
public sealed record PhotoSitemapPhoto(int PicId, DateTime DateTime);

/// <summary>
/// One visible photography category and its detail URLs for the core sitemap.
/// </summary>
public sealed record PhotoSitemapCategory(
    int CatId,
    string Name,
    string Slug,
    IReadOnlyList<PhotoSitemapPhoto> Photos);
