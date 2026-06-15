using Dapper;
using Microsoft.Data.SqlClient;

namespace QueenZone.Data;

public sealed class LegacyNewsRepository(string connectionString) : INewsRepository
{
    private const string PublishedNewsCte = """
        WITH PublishedNews AS (
            SELECT
                NEWS_ID AS Id,
                TITLE AS Title,
                SLUG AS Slug,
                ISNULL(EXCERPT, '') AS Excerpt,
                ISNULL(ARTICLE, '') AS Body,
                [DATE] AS PublishedAt,
                SOURCE_URL AS SourceUrl,
                CAST(CASE WHEN DISPLAY = 1 THEN 1 ELSE 0 END AS bit) AS IsPublished,
                ROW_NUMBER() OVER (PARTITION BY NEWS_ID ORDER BY [DATE] DESC, NEWS_ID DESC) AS RowNumber
            FROM NEWS_T
            WHERE DISPLAY = 1
        )
        """;

    public async Task<IReadOnlyList<NewsItem>> GetLatestAsync(int count, CancellationToken cancellationToken = default)
    {
        var sql = $"""
            {PublishedNewsCte}
            SELECT TOP (@Count)
                Id,
                Title,
                Slug,
                Excerpt,
                Body,
                PublishedAt,
                SourceUrl,
                IsPublished
            FROM PublishedNews
            WHERE RowNumber = 1
            ORDER BY PublishedAt DESC, Id DESC
            """;

        return await QueryAsync(sql, new { Count = count }, cancellationToken);
    }

    public async Task<int> GetPublishedCountAsync(CancellationToken cancellationToken = default)
    {
        var sql = $"""
            {PublishedNewsCte}
            SELECT COUNT(*)
            FROM PublishedNews
            WHERE RowNumber = 1
            """;

        await using var connection = new SqlConnection(connectionString);
        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
        return await connection.ExecuteScalarAsync<int>(command);
    }

    public async Task<IReadOnlyList<NewsItem>> GetArchivePageAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var sql = $"""
            {PublishedNewsCte}
            SELECT
                Id,
                Title,
                Slug,
                Excerpt,
                Body,
                PublishedAt,
                SourceUrl,
                IsPublished
            FROM PublishedNews
            WHERE RowNumber = 1
            ORDER BY PublishedAt DESC, Id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        var offset = Math.Max(page - 1, 0) * pageSize;
        return await QueryAsync(sql, new { Offset = offset, PageSize = pageSize }, cancellationToken);
    }

    public async Task<NewsItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var sql = $"""
            {PublishedNewsCte}
            SELECT
                Id,
                Title,
                Slug,
                Excerpt,
                Body,
                PublishedAt,
                SourceUrl,
                IsPublished
            FROM PublishedNews
            WHERE RowNumber = 1
              AND Id = @Id
            """;

        var results = await QueryAsync(sql, new { Id = id }, cancellationToken);
        return results.FirstOrDefault();
    }

    private async Task<IReadOnlyList<NewsItem>> QueryAsync(string sql, object parameters, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        var command = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        var items = await connection.QueryAsync<NewsItem>(command);
        return items.AsList();
    }
}