using Dapper;
using Microsoft.Data.SqlClient;

namespace QueenZone.Data;

public sealed class LegacyNewsRepository(string connectionString) : INewsRepository
{
    public async Task<IReadOnlyList<NewsItem>> GetLatestAsync(int count, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT TOP (@Count)
                NEWS_ID AS Id,
                TITLE AS Title,
                ISNULL(EXCERPT, '') AS Excerpt,
                ISNULL(ARTICLE, '') AS Body,
                [DATE] AS PublishedAt,
                SOURCE_URL AS SourceUrl,
                CAST(CASE WHEN DISPLAY = 1 THEN 1 ELSE 0 END AS bit) AS IsPublished
            FROM NEWS_T
            WHERE DISPLAY = 1
            ORDER BY [DATE] DESC, NEWS_ID DESC
            """;

        return await QueryAsync(sql, new { Count = count }, cancellationToken);
    }

    public async Task<IReadOnlyList<NewsItem>> GetArchivePageAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                NEWS_ID AS Id,
                TITLE AS Title,
                ISNULL(EXCERPT, '') AS Excerpt,
                ISNULL(ARTICLE, '') AS Body,
                [DATE] AS PublishedAt,
                SOURCE_URL AS SourceUrl,
                CAST(CASE WHEN DISPLAY = 1 THEN 1 ELSE 0 END AS bit) AS IsPublished
            FROM NEWS_T
            WHERE DISPLAY = 1
            ORDER BY [DATE] DESC, NEWS_ID DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        var offset = Math.Max(page - 1, 0) * pageSize;
        return await QueryAsync(sql, new { Offset = offset, PageSize = pageSize }, cancellationToken);
    }

    public async Task<NewsItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                NEWS_ID AS Id,
                TITLE AS Title,
                ISNULL(EXCERPT, '') AS Excerpt,
                ISNULL(ARTICLE, '') AS Body,
                [DATE] AS PublishedAt,
                SOURCE_URL AS SourceUrl,
                CAST(CASE WHEN DISPLAY = 1 THEN 1 ELSE 0 END AS bit) AS IsPublished
            FROM NEWS_T
            WHERE NEWS_ID = @Id
              AND DISPLAY = 1
            """;

        var results = await QueryAsync(sql, new { Id = id }, cancellationToken);
        return results.SingleOrDefault();
    }

    private async Task<IReadOnlyList<NewsItem>> QueryAsync(string sql, object parameters, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        var command = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        var items = await connection.QueryAsync<NewsItem>(command);
        return items.AsList();
    }
}
