namespace QueenZone.Data;

public sealed class InMemoryPhotoRepository(SharedPhotoStore store) : IPhotoRepository
{
    public Task<IReadOnlyList<PhotoCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PhotoCategory> categories = store.GetCategories()
            .Select(category =>
            {
                var count = store.GetVisiblePhotosByCategory(category.CatId).Count;
                return new { category, count };
            })
            .Where(item => item.count > 0)
            .Select(item => new PhotoCategory(item.category.CatId, item.category.Name, item.category.Slug, item.count))
            .ToList();

        return Task.FromResult(categories);
    }

    public async Task<PhotoCategory?> GetCategoryBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var categories = await GetCategoriesAsync(cancellationToken);
        return categories.FirstOrDefault(category =>
            string.Equals(category.Slug, slug, StringComparison.OrdinalIgnoreCase));
    }

    public Task<PhotoCategoryPage> GetCategoryPageAsync(
        int catId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var category = store.GetCategory(catId);
        if (category is null)
        {
            return Task.FromResult(new PhotoCategoryPage(string.Empty, [], 0));
        }

        var items = store.GetVisiblePhotosByCategory(catId).Select(ToPhotoItem).ToList();
        var paged = items
            .Skip(Math.Max(page - 1, 0) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult(new PhotoCategoryPage(category.Name, paged, items.Count));
    }

    public Task<IReadOnlyList<PhotoItem>> GetCategoryAllAsync(int catId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PhotoItem> items = store.GetVisiblePhotosByCategory(catId).Select(ToPhotoItem).ToList();
        return Task.FromResult(items);
    }

    public Task<IReadOnlyList<PhotoSitemapCategory>> GetPublishedSitemapCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PhotoSitemapCategory> categories = store.GetCategories()
            .Select(category =>
            {
                IReadOnlyList<PhotoSitemapPhoto> photos = store.GetVisiblePhotosByCategory(category.CatId)
                    .Select(item => new PhotoSitemapPhoto(item.PicId, item.DateTime))
                    .ToList();
                return new { category, photos };
            })
            .Where(item => item.photos.Count > 0)
            .Select(item => new PhotoSitemapCategory(
                item.category.CatId,
                item.category.Name,
                item.category.Slug,
                item.photos))
            .ToList();

        return Task.FromResult(categories);
    }

    private static PhotoItem ToPhotoItem(AdminPhotoItem item) =>
        new(
            item.PicId,
            item.CatId,
            item.CategoryName,
            item.CategorySlug,
            item.Title,
            item.ImageUrl,
            item.ThumbnailUrl,
            item.ThumbWidth,
            item.ThumbHeight,
            item.Year,
            item.DateTime);
}
