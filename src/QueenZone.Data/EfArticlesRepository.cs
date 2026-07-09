using Microsoft.EntityFrameworkCore;

namespace QueenZone.Data;

/// <summary>
/// Public article archive reads against legacy tables via EF Core SQL queries.
/// </summary>
public sealed class EfArticlesRepository(QueenZoneDbContext dbContext) : IArticlesRepository
{
    private const string PublishedArticlesSelect = """
        SELECT
            CAST(a.Q_ARTICLE_ID AS int) AS Id,
            a.ARTICLE_NAME AS Title,
            ISNULL(a.ARTICLE_TEXT, '') AS Body,
            a.DATE_CREATED AS PublishedAt,
            NULLIF(LTRIM(RTRIM(a.SOURCE)), '') AS Source,
            NULLIF(LTRIM(RTRIM(c.ARTICLE_CATEGORY)), '') AS CategoryName,
            CAST(CASE WHEN a.DISPLAY = 1 THEN 1 ELSE 0 END AS bit) AS IsPublished
        FROM Q_ARTICLE_T a
        LEFT JOIN Q_ARTICLE_CATEGORY_T c
            ON c.Q_ARTICLE_CAT_ID = a.Q_ARTICLE_CATEGORY_ID
        WHERE a.DISPLAY = 1
        """;

    public async Task<IReadOnlyList<ArticleItem>> GetLatestAsync(int count, CancellationToken cancellationToken = default)
    {
        var sql = PublishedArticlesSelect + """

            ORDER BY a.DATE_CREATED DESC, a.Q_ARTICLE_ID DESC
            OFFSET 0 ROWS FETCH NEXT {0} ROWS ONLY
            """;

        var rows = await dbContext.Database
            .SqlQueryRaw<ArticleRow>(sql, count)
            .ToListAsync(cancellationToken);
        return rows.Select(Map).ToList();
    }

    public async Task<int> GetPublishedCountAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(*) AS Value
            FROM Q_ARTICLE_T
            WHERE DISPLAY = 1
            """;

        return await dbContext.Database
            .SqlQueryRaw<int>(sql)
            .FirstAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ArticleItem>> GetArchivePageAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var offset = Math.Max(page - 1, 0) * pageSize;
        var sql = PublishedArticlesSelect + """

            ORDER BY a.DATE_CREATED DESC, a.Q_ARTICLE_ID DESC
            OFFSET {0} ROWS FETCH NEXT {1} ROWS ONLY
            """;

        var rows = await dbContext.Database
            .SqlQueryRaw<ArticleRow>(sql, offset, pageSize)
            .ToListAsync(cancellationToken);
        return rows.Select(Map).ToList();
    }

    public async Task<ArticleItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var sql = PublishedArticlesSelect + """

              AND a.Q_ARTICLE_ID = {0}
            """;

        var rows = await dbContext.Database
            .SqlQueryRaw<ArticleRow>(sql, id)
            .ToListAsync(cancellationToken);
        var row = rows.FirstOrDefault();
        return row is null ? null : Map(row);
    }

    public async Task<IReadOnlyList<SitemapContentEntry>> GetPublishedSitemapEntriesAsync(
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                CAST(a.Q_ARTICLE_ID AS int) AS Id,
                a.ARTICLE_NAME AS Title,
                a.DATE_CREATED AS PublishedAt
            FROM Q_ARTICLE_T a
            WHERE a.DISPLAY = 1
            ORDER BY a.DATE_CREATED DESC, a.Q_ARTICLE_ID DESC
            """;

        return await dbContext.Database
            .SqlQueryRaw<SitemapContentEntry>(sql)
            .ToListAsync(cancellationToken);
    }

    private static ArticleItem Map(ArticleRow row) =>
        new(
            row.Id,
            row.Title,
            LegacyArticleText.GetExcerpt(row.Body),
            row.Body,
            row.PublishedAt,
            row.Source,
            row.CategoryName,
            row.IsPublished);

    internal sealed class ArticleRow
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Body { get; set; } = string.Empty;

        public DateTime PublishedAt { get; set; }

        public string? Source { get; set; }

        public string? CategoryName { get; set; }

        public bool IsPublished { get; set; }
    }
}
