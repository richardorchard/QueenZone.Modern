using Dapper;
using Microsoft.Data.SqlClient;

namespace QueenZone.Data;

public sealed class LegacyNewsRepository : INewsRepository
{
    private readonly string connectionString;
    private readonly string publishedNewsCte;

    public LegacyNewsRepository(string connectionString)
    {
        this.connectionString = connectionString;
        publishedNewsCte = LegacyNewsSchema.BuildPublishedNewsCte(LegacyNewsSchema.HasSlugColumn(connectionString));
    }

    public async Task<IReadOnlyList<NewsItem>> GetLatestAsync(int count, CancellationToken cancellationToken = default)
    {
        var sql = $"""
            {publishedNewsCte}
            SELECT TOP (@Count)
                Id,
                Title,
                Excerpt,
                Body,
                PublishedAt,
                SourceUrl,
                IsPublished,
                Slug
            FROM PublishedNews
            WHERE RowNumber = 1
            ORDER BY PublishedAt DESC, Id DESC -- legacy [DATE] descending, then NEWS_ID
            """;

        return await QueryAsync(sql, new { Count = count }, cancellationToken);
    }

    public async Task<int> GetPublishedCountAsync(CancellationToken cancellationToken = default)
    {
        var sql = $"""
            {publishedNewsCte}
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
            {publishedNewsCte}
            SELECT
                Id,
                Title,
                Excerpt,
                Body,
                PublishedAt,
                SourceUrl,
                IsPublished,
                Slug
            FROM PublishedNews
            WHERE RowNumber = 1
            ORDER BY PublishedAt DESC, Id DESC -- legacy [DATE] descending, then NEWS_ID
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        var offset = Math.Max(page - 1, 0) * pageSize;
        return await QueryAsync(sql, new { Offset = offset, PageSize = pageSize }, cancellationToken);
    }

    public async Task<NewsItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var sql = $"""
            {publishedNewsCte}
            SELECT
                Id,
                Title,
                Excerpt,
                Body,
                PublishedAt,
                SourceUrl,
                IsPublished,
                Slug
            FROM PublishedNews
            WHERE RowNumber = 1
              AND Id = @Id
            """;

        var results = await QueryAsync(sql, new { Id = id }, cancellationToken);
        return results.FirstOrDefault();
    }

    public async Task<IReadOnlyList<SitemapContentEntry>> GetPublishedSitemapEntriesAsync(CancellationToken cancellationToken = default)
    {
        var sql = $"""
            {publishedNewsCte}
            SELECT
                Id,
                Title,
                PublishedAt,
                Slug
            FROM PublishedNews
            WHERE RowNumber = 1
            ORDER BY PublishedAt DESC, Id DESC
            """;

        await using var connection = new SqlConnection(connectionString);
        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<SitemapContentEntry>(command);
        return rows.AsList();
    }

    private async Task<IReadOnlyList<NewsItem>> QueryAsync(string sql, object parameters, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        var command = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        var items = await connection.QueryAsync<NewsItem>(command);
        return items.AsList();
    }
}