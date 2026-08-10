namespace QueenZone.Data;

public interface IPhotoRepository
{
    Task<IReadOnlyList<PhotoCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    Task<PhotoCategory?> GetCategoryBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<PhotoCategoryPage> GetCategoryPageAsync(
        int catId,
        int page,
        int pageSize,
        PhotoListFilter? filter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads one photo plus prev/next ids and position without materializing the category.
    /// When <paramref name="filter"/> is active, totals/neighbors are restricted to matches
    /// and the current photo must match the filter (otherwise null).
    /// </summary>
    Task<PhotoDetailNavigation?> GetDetailNavigationAsync(
        int catId,
        int picId,
        PhotoListFilter? filter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Full visible collection for tools/inventory. Prefer
    /// <see cref="GetDetailNavigationAsync"/> for public detail pages.
    /// </summary>
    Task<IReadOnlyList<PhotoItem>> GetCategoryAllAsync(int catId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns up to <paramref name="take"/> published photos from a category in random order
    /// without materializing the full category (SQL <c>TOP</c> / <c>LIMIT</c> + random order).
    /// </summary>
    Task<IReadOnlyList<PhotoItem>> GetRandomPublishedInCategoryAsync(
        int catId,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns visible categories and photo detail ids/dates in one repository pass
    /// for sitemap generation (avoids a second full category reload in the builder).
    /// </summary>
    Task<IReadOnlyList<PhotoSitemapCategory>> GetPublishedSitemapCategoriesAsync(
        CancellationToken cancellationToken = default);
}
