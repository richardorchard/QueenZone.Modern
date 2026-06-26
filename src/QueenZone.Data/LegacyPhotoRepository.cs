using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;

namespace QueenZone.Data;

/// <summary>
/// Reads the legacy picture library via its original stored procedures
/// (Q_PICTURE_CATEGORY_SP, Q_PIC_CAT_PAGE4_SP) rather than new inline SQL,
/// since those procs already encode the DISPLAY=1 visibility rule and category join.
/// </summary>
public sealed class LegacyPhotoRepository : IPhotoRepository
{
    // Largest observed category is ~1,900 images; this caps the temp-table page
    // size Q_PIC_CAT_PAGE4_SP is asked to build so a single call returns the
    // whole (DISTINCT, joined) collection.
    private const int MaxCollectionSize = 5000;

    private readonly string connectionString;

    public LegacyPhotoRepository(string connectionString)
    {
        this.connectionString = connectionString;
    }

    public async Task<IReadOnlyList<PhotoCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        var command = new CommandDefinition(
            "Q_PICTURE_CATEGORY_SP",
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<CategoryRow>(command);

        var categories = new List<PhotoCategory>();
        foreach (var row in rows)
        {
            var items = await QueryCategoryCollectionAsync(connection, row.cat_id, cancellationToken);
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
        return categories.FirstOrDefault(category => string.Equals(category.Slug, slug, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<PhotoCategoryPage> GetCategoryPageAsync(int catId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        var items = await QueryCategoryCollectionAsync(connection, catId, cancellationToken);
        var categoryName = items.Count > 0 ? items[0].CategoryName : string.Empty;

        var paged = items
            .Skip(Math.Max(page - 1, 0) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PhotoCategoryPage(categoryName, paged, items.Count);
    }

    public async Task<IReadOnlyList<PhotoItem>> GetCategoryAllAsync(int catId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        return await QueryCategoryCollectionAsync(connection, catId, cancellationToken);
    }

    /// <summary>
    /// Fetches every visible image in a category in one call.
    /// <c>Q_PIC_CAT_PAGE4_SP</c>'s own <c>TotalRecords</c> output param counts raw
    /// PIC_FILES_T rows, which can exceed the number of DISTINCT joined rows it
    /// actually returns — trusting it for pagination math can produce a final
    /// page with fewer (or zero) items than promised. Using the real row count
    /// here keeps category counts, grid pagination, and lightbox navigation
    /// (all backed by this method) consistent with each other.
    /// </summary>
    private static async Task<List<PhotoItem>> QueryCategoryCollectionAsync(
        SqlConnection connection,
        int catId,
        CancellationToken cancellationToken)
    {
        var parameters = new DynamicParameters();
        parameters.Add("CurrentPage", 1);
        parameters.Add("PageSize", MaxCollectionSize);
        parameters.Add("CAT_ID", catId);
        parameters.Add("TotalRecords", dbType: DbType.Int32, direction: ParameterDirection.Output);
        parameters.Add("CATEGORY_NAME", dbType: DbType.String, size: 50, direction: ParameterDirection.Output);

        var command = new CommandDefinition(
            "Q_PIC_CAT_PAGE4_SP",
            parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        var rows = (await connection.QueryAsync<CategoryPageRow>(command)).AsList();

        var categoryName = parameters.Get<string?>("CATEGORY_NAME") ?? rows.FirstOrDefault()?.category_name ?? string.Empty;
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

    private sealed class CategoryRow
    {
        public int cat_id { get; set; }

        public string name { get; set; } = string.Empty;
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
}
