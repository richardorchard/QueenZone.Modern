using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace QueenZone.Data;

/// <summary>
/// Reads the legacy picture library via its original stored procedures
/// (<c>Q_PICTURE_CATEGORY_SP</c>, <c>Q_PIC_CAT_PAGE4_SP</c>) through EF Core rather than Dapper.
/// Those procs already encode the DISPLAY=1 visibility rule and category join.
/// </summary>
public sealed class EfPhotoRepository : IPhotoRepository
{
    // Largest observed category is ~1,900 images; this caps the temp-table page
    // size Q_PIC_CAT_PAGE4_SP is asked to build so a single call returns the
    // whole (DISTINCT, joined) collection.
    private const int MaxCollectionSize = 5000;

    private readonly QueenZoneDbContext dbContext;
    private readonly bool useLegacyProcedures;
    private readonly string categoriesSql;
    private readonly Func<int, string> categoryPageSql;

    [ExcludeFromCodeCoverage] // Production stored-procedure wiring; methods covered via test SQL hooks.
    public EfPhotoRepository(QueenZoneDbContext dbContext)
        : this(
            dbContext,
            useLegacyProcedures: true,
            categoriesSql: string.Empty,
            categoryPageSql: static _ => string.Empty)
    {
    }

    internal EfPhotoRepository(
        QueenZoneDbContext dbContext,
        bool useLegacyProcedures,
        string categoriesSql,
        Func<int, string> categoryPageSql)
    {
        this.dbContext = dbContext;
        this.useLegacyProcedures = useLegacyProcedures;
        this.categoriesSql = categoriesSql;
        this.categoryPageSql = categoryPageSql;
    }

    public async Task<IReadOnlyList<PhotoCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CategoryRow> rows;
        if (useLegacyProcedures)
        {
            rows = await QueryCategoriesViaProcedureAsync(cancellationToken);
        }
        else
        {
            rows = await dbContext.Database
                .SqlQueryRaw<CategoryRow>(categoriesSql)
                .ToListAsync(cancellationToken);
        }

        var categories = new List<PhotoCategory>();
        foreach (var row in rows)
        {
            var items = await QueryCategoryCollectionAsync(row.cat_id, cancellationToken);
            if (items.Count > 0)
            {
                categories.Add(new PhotoCategory(row.cat_id, row.name, NewsSlug.Slugify(row.name), items.Count));
            }
        }

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
        var items = await QueryCategoryCollectionAsync(catId, cancellationToken);
        var categoryName = items.Count > 0 ? items[0].CategoryName : string.Empty;

        var paged = items
            .Skip(Math.Max(page - 1, 0) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PhotoCategoryPage(categoryName, paged, items.Count);
    }

    public async Task<IReadOnlyList<PhotoItem>> GetCategoryAllAsync(
        int catId,
        CancellationToken cancellationToken = default) =>
        await QueryCategoryCollectionAsync(catId, cancellationToken);

    /// <summary>
    /// Fetches every visible image in a category in one call.
    /// <c>Q_PIC_CAT_PAGE4_SP</c>'s own <c>TotalRecords</c> output param counts raw
    /// PIC_FILES_T rows, which can exceed the number of DISTINCT joined rows it
    /// actually returns — trusting it for pagination math can produce a final
    /// page with fewer (or zero) items than promised. Using the real row count
    /// here keeps category counts, grid pagination, and lightbox navigation
    /// consistent with each other.
    /// </summary>
    private async Task<List<PhotoItem>> QueryCategoryCollectionAsync(
        int catId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<CategoryPageRow> rows;
        string? outputCategoryName = null;

        if (useLegacyProcedures)
        {
            (rows, outputCategoryName) = await QueryCategoryPageViaProcedureAsync(catId, cancellationToken);
        }
        else
        {
            rows = await dbContext.Database
                .SqlQueryRaw<CategoryPageRow>(categoryPageSql(catId))
                .ToListAsync(cancellationToken);
        }

        var categoryName = outputCategoryName
            ?? rows.FirstOrDefault()?.category_name
            ?? string.Empty;
        var categorySlug = NewsSlug.Slugify(categoryName);

        return rows
            .Select(row => new PhotoItem(
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
                DateTime: row.DATE_TIME))
            .ToList();
    }

    [ExcludeFromCodeCoverage]
    private Task<IReadOnlyList<CategoryRow>> QueryCategoriesViaProcedureAsync(
        CancellationToken cancellationToken) =>
        EfSql.QueryProcAsync<CategoryRow>(
            dbContext,
            "Q_PICTURE_CATEGORY_SP",
            cancellationToken: cancellationToken);

    [ExcludeFromCodeCoverage]
    private async Task<(IReadOnlyList<CategoryPageRow> Rows, string? CategoryName)> QueryCategoryPageViaProcedureAsync(
        int catId,
        CancellationToken cancellationToken)
    {
        var categoryNameParam = EfSql.OutputString("@CATEGORY_NAME", 50);
        var rows = await EfSql.QueryProcAsync<CategoryPageRow>(
            dbContext,
            "Q_PIC_CAT_PAGE4_SP",
            command =>
            {
                command.Parameters.Add(EfSql.Input("@CurrentPage", 1));
                command.Parameters.Add(EfSql.Input("@PageSize", MaxCollectionSize));
                command.Parameters.Add(EfSql.Input("@CAT_ID", catId));
                command.Parameters.Add(EfSql.OutputInt("@TotalRecords"));
                command.Parameters.Add(categoryNameParam);
            },
            cancellationToken: cancellationToken);
        return (rows, EfSql.GetNullableString(categoryNameParam));
    }

    internal sealed class CategoryRow
    {
        public int cat_id { get; set; }

        public string name { get; set; } = string.Empty;
    }

    internal sealed class CategoryPageRow
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
}
