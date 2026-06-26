namespace QueenZone.Data;

public sealed class InMemoryPhotoRepository : IPhotoRepository
{
    private readonly IReadOnlyList<PhotoCategorySeed> seedCategories;

    public InMemoryPhotoRepository(IReadOnlyList<PhotoCategorySeed> seedCategories)
    {
        this.seedCategories = seedCategories;
    }

    public Task<IReadOnlyList<PhotoCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PhotoCategory> categories = seedCategories
            .Where(seed => seed.Items.Count > 0)
            .Select(seed => new PhotoCategory(seed.CatId, seed.Name, NewsSlug.Slugify(seed.Name), seed.Items.Count))
            .ToList();

        return Task.FromResult(categories);
    }

    public async Task<PhotoCategory?> GetCategoryBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var categories = await GetCategoriesAsync(cancellationToken);
        return categories.FirstOrDefault(category => string.Equals(category.Slug, slug, StringComparison.OrdinalIgnoreCase));
    }

    public Task<PhotoCategoryPage> GetCategoryPageAsync(int catId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var seed = seedCategories.FirstOrDefault(s => s.CatId == catId);
        if (seed is null)
        {
            return Task.FromResult(new PhotoCategoryPage(string.Empty, [], 0));
        }

        var items = MapItems(seed).ToList();
        var paged = items
            .Skip(Math.Max(page - 1, 0) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult(new PhotoCategoryPage(seed.Name, paged, items.Count));
    }

    public Task<IReadOnlyList<PhotoItem>> GetCategoryAllAsync(int catId, CancellationToken cancellationToken = default)
    {
        var seed = seedCategories.FirstOrDefault(s => s.CatId == catId);
        IReadOnlyList<PhotoItem> items = seed is null ? [] : MapItems(seed).ToList();
        return Task.FromResult(items);
    }

    private static IEnumerable<PhotoItem> MapItems(PhotoCategorySeed seed)
    {
        var slug = NewsSlug.Slugify(seed.Name);
        return seed.Items.Select(item => new PhotoItem(
            PicId: item.PicId,
            CatId: seed.CatId,
            CategoryName: seed.Name,
            CategorySlug: slug,
            Title: item.Title,
            ImageUrl: PhotoImageUrl.Build(item.Url),
            ThumbnailUrl: PhotoImageUrl.Build(item.ThumbUrl),
            ThumbWidth: 150,
            ThumbHeight: 150,
            Year: item.DateTime.Year,
            DateTime: item.DateTime));
    }
}
