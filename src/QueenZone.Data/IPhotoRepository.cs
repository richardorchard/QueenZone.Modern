namespace QueenZone.Data;

public interface IPhotoRepository
{
    Task<IReadOnlyList<PhotoCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    Task<PhotoCategory?> GetCategoryBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<PhotoCategoryPage> GetCategoryPageAsync(int catId, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PhotoItem>> GetCategoryAllAsync(int catId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns visible categories and photo detail ids/dates in one repository pass
    /// for sitemap generation (avoids a second full category reload in the builder).
    /// </summary>
    Task<IReadOnlyList<PhotoSitemapCategory>> GetPublishedSitemapCategoriesAsync(
        CancellationToken cancellationToken = default);
}
