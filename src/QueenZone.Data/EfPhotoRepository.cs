using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace QueenZone.Data;

/// <summary>
/// Reads the legacy picture library with targeted SQL (counts, server-side paging,
/// neighbor navigation) instead of loading whole categories via
/// <c>Q_PIC_CAT_PAGE4_SP</c> (which materializes every visible row into a temp table).
/// </summary>
public sealed class EfPhotoRepository : IPhotoRepository
{
    private readonly QueenZoneDbContext dbContext;
    private readonly PhotoSqlQueries sql;

    [ExcludeFromCodeCoverage]
    public EfPhotoRepository(QueenZoneDbContext dbContext)
        : this(dbContext, PhotoSqlQueries.CreateProduction())
    {
    }

    internal EfPhotoRepository(QueenZoneDbContext dbContext, PhotoSqlQueries sql)
    {
        this.dbContext = dbContext;
        this.sql = sql;
    }

    public async Task<IReadOnlyList<PhotoCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.Database
            .SqlQueryRaw<CategoryWithCountRow>(sql.CategoriesWithCountsSql)
            .ToListAsync(cancellationToken);

        IReadOnlyList<PhotoCategory> categories = rows
            .Select(row => new PhotoCategory(
                row.cat_id,
                row.name,
                NewsSlug.Slugify(row.name),
                row.ImageCount,
                string.IsNullOrWhiteSpace(row.CoverThumbUrl) ? null : PhotoImageUrl.Build(row.CoverThumbUrl)))
            .ToList();
        return categories;
    }

    public async Task<PhotoCategory?> GetCategoryBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var categories = await GetCategoriesAsync(cancellationToken);
        return categories.FirstOrDefault(category =>
            string.Equals(category.Slug, slug, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<PhotoCategoryPage> GetCategoryPageAsync(
        int catId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var safePage = Math.Max(page, 1);
        var safePageSize = Math.Clamp(pageSize, 1, 200);
        var offset = (safePage - 1) * safePageSize;

        var total = await dbContext.Database
            .SqlQueryRaw<IntValueRow>(sql.CategoryCountSql, catId)
            .SingleAsync(cancellationToken);

        var nameRows = await dbContext.Database
            .SqlQueryRaw<NameRow>(sql.CategoryNameSql, catId)
            .ToListAsync(cancellationToken);
        var categoryName = nameRows.FirstOrDefault()?.name ?? string.Empty;
        var categorySlug = NewsSlug.Slugify(categoryName);

        var rows = await dbContext.Database
            .SqlQueryRaw<CategoryPageRow>(sql.CategoryPageSql, offset, safePageSize, catId)
            .ToListAsync(cancellationToken);

        var items = rows.Select(row => MapItem(row, catId, categoryName, categorySlug)).ToList();
        return new PhotoCategoryPage(categoryName, items, total.Value);
    }

    public async Task<PhotoDetailNavigation?> GetDetailNavigationAsync(
        int catId,
        int picId,
        CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.Database
            .SqlQueryRaw<CategoryPageRow>(sql.PhotoByIdSql, catId, picId)
            .ToListAsync(cancellationToken);
        var row = rows.FirstOrDefault();
        if (row is null)
        {
            return null;
        }

        var categoryName = row.category_name ?? string.Empty;
        var categorySlug = NewsSlug.Slugify(categoryName);
        var photo = MapItem(row, catId, categoryName, categorySlug);

        var total = await dbContext.Database
            .SqlQueryRaw<IntValueRow>(sql.CategoryCountSql, catId)
            .SingleAsync(cancellationToken);

        var index = await dbContext.Database
            .SqlQueryRaw<IntValueRow>(sql.IndexBeforeSql, catId, row.DATE_TIME, picId)
            .SingleAsync(cancellationToken);

        var previousRows = await dbContext.Database
            .SqlQueryRaw<IntValueRow>(sql.PreviousPicIdSql, catId, row.DATE_TIME, picId)
            .ToListAsync(cancellationToken);
        var nextRows = await dbContext.Database
            .SqlQueryRaw<IntValueRow>(sql.NextPicIdSql, catId, row.DATE_TIME, picId)
            .ToListAsync(cancellationToken);

        return new PhotoDetailNavigation(
            photo,
            index.Value,
            total.Value,
            previousRows.FirstOrDefault()?.Value,
            nextRows.FirstOrDefault()?.Value);
    }

    public async Task<IReadOnlyList<PhotoItem>> GetCategoryAllAsync(
        int catId,
        CancellationToken cancellationToken = default)
    {
        var nameRows = await dbContext.Database
            .SqlQueryRaw<NameRow>(sql.CategoryNameSql, catId)
            .ToListAsync(cancellationToken);
        var categoryName = nameRows.FirstOrDefault()?.name ?? string.Empty;
        var categorySlug = NewsSlug.Slugify(categoryName);

        var rows = await dbContext.Database
            .SqlQueryRaw<CategoryPageRow>(sql.CategoryAllSql, catId)
            .ToListAsync(cancellationToken);

        IReadOnlyList<PhotoItem> items = rows
            .Select(row => MapItem(row, catId, categoryName, categorySlug))
            .ToList();
        return items;
    }

    public async Task<IReadOnlyList<PhotoSitemapCategory>> GetPublishedSitemapCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.Database
            .SqlQueryRaw<SitemapRow>(sql.SitemapSql)
            .ToListAsync(cancellationToken);

        IReadOnlyList<PhotoSitemapCategory> categories = rows
            .GroupBy(row => row.cat_id)
            .Select(group =>
            {
                var first = group.First();
                var name = first.category_name;
                IReadOnlyList<PhotoSitemapPhoto> photos = group
                    .Select(row => new PhotoSitemapPhoto(row.pic_id, row.date_time))
                    .ToList();
                return new PhotoSitemapCategory(group.Key, name, NewsSlug.Slugify(name), photos);
            })
            .ToList();

        return categories;
    }

    private static PhotoItem MapItem(CategoryPageRow row, int catId, string categoryName, string categorySlug) =>
        new(
            PicId: row.pic_id,
            CatId: catId,
            CategoryName: categoryName,
            CategorySlug: categorySlug,
            Title: row.NAME,
            ImageUrl: PhotoImageUrl.Build(row.URL),
            ThumbnailUrl: PhotoImageUrl.Build(row.THUMB_URL),
            ThumbWidth: row.T_WIDTH,
            ThumbHeight: row.T_HEIGHT,
            Year: row.DATE_TIME.Year,
            DateTime: row.DATE_TIME);

    private sealed class CategoryWithCountRow
    {
        public int cat_id { get; set; }

        public string name { get; set; } = string.Empty;

        public int ImageCount { get; set; }

        public string? CoverThumbUrl { get; set; }
    }

    private sealed class CategoryPageRow
    {
        public string NAME { get; set; } = string.Empty;

        public DateTime DATE_TIME { get; set; }

        public string URL { get; set; } = string.Empty;

        public string THUMB_URL { get; set; } = string.Empty;

        public int T_HEIGHT { get; set; }

        public int T_WIDTH { get; set; }

        public int pic_id { get; set; }

        public string? category_name { get; set; }
    }

    private sealed class NameRow
    {
        public string name { get; set; } = string.Empty;
    }

    private sealed class IntValueRow
    {
        public int Value { get; set; }
    }

    private sealed class SitemapRow
    {
        public int cat_id { get; set; }

        public string category_name { get; set; } = string.Empty;

        public int pic_id { get; set; }

        public DateTime date_time { get; set; }
    }
}
