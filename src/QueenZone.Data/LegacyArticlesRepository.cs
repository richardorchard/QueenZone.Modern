using Dapper;
using Microsoft.Data.SqlClient;

namespace QueenZone.Data;

public sealed class LegacyArticlesRepository(string connectionString) : IArticlesRepository
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
        var sql = $"""
            {PublishedArticlesSelect}
            ORDER BY a.DATE_CREATED DESC, a.Q_ARTICLE_ID DESC
            OFFSET 0 ROWS FETCH NEXT @Count ROWS ONLY
            """;

        return await QueryAsync(sql, new { Count = count }, cancellationToken);
    }

    public async Task<int> GetPublishedCountAsync(CancellationToken cancellationToken = default)
    {
        var sql = """
            SELECT COUNT(*)
            FROM Q_ARTICLE_T
            WHERE DISPLAY = 1
            """;

        await using var connection = new SqlConnection(connectionString);
        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
        return await connection.ExecuteScalarAsync<int>(command);
    }

    public async Task<IReadOnlyList<ArticleItem>> GetArchivePageAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var sql = $"""
            {PublishedArticlesSelect}
            ORDER BY a.DATE_CREATED DESC, a.Q_ARTICLE_ID DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        var offset = Math.Max(page - 1, 0) * pageSize;
        return await QueryAsync(sql, new { Offset = offset, PageSize = pageSize }, cancellationToken);
    }

    public async Task<ArticleItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var sql = $"""
            {PublishedArticlesSelect}
              AND a.Q_ARTICLE_ID = @Id
            """;

        var results = await QueryAsync(sql, new { Id = id }, cancellationToken);
        return results.FirstOrDefault();
    }

    private async Task<IReadOnlyList<ArticleItem>> QueryAsync(string sql, object parameters, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        var command = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<ArticleRow>(command);
        return rows.Select(Map).ToList();
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

    private sealed record ArticleRow(
        int Id,
        string Title,
        string Body,
        DateTime PublishedAt,
        string? Source,
        string? CategoryName,
        bool IsPublished);
}