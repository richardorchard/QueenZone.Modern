using Dapper;
using Microsoft.Data.SqlClient;

namespace QueenZone.Data;

public sealed class LegacyAdminNewsRepository(string connectionString) : IAdminNewsRepository
{
    private const string LatestNewsCte = """
        WITH LatestNews AS (
            SELECT
                NEWS_ID AS Id,
                TITLE AS Title,
                SLUG AS Slug,
                ISNULL(EXCERPT, '') AS Excerpt,
                ISNULL(ARTICLE, '') AS Body,
                [DATE] AS PublishedAt,
                SOURCE_URL AS SourceUrl,
                CAST(CASE WHEN DISPLAY = 1 THEN 1 ELSE 0 END AS bit) AS IsPublished,
                CREATED_AT AS CreatedAt,
                UPDATED_AT AS UpdatedAt,
                EDITOR_EMAIL AS EditorEmail,
                ROW_NUMBER() OVER (PARTITION BY NEWS_ID ORDER BY [DATE] DESC, NEWS_ID DESC) AS RowNumber
            FROM NEWS_T
        )
        """;

    public async Task<IReadOnlyList<AdminNewsArticle>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var sql = $"""
            {LatestNewsCte}
            SELECT
                Id,
                Title,
                Slug,
                Excerpt,
                Body,
                PublishedAt,
                SourceUrl,
                IsPublished,
                CreatedAt,
                UpdatedAt,
                EditorEmail
            FROM LatestNews
            WHERE RowNumber = 1
            ORDER BY PublishedAt DESC, Id DESC
            """;

        return await QueryAdminAsync(sql, new { }, cancellationToken);
    }

    public async Task<AdminNewsArticle?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var sql = $"""
            {LatestNewsCte}
            SELECT
                Id,
                Title,
                Slug,
                Excerpt,
                Body,
                PublishedAt,
                SourceUrl,
                IsPublished,
                CreatedAt,
                UpdatedAt,
                EditorEmail
            FROM LatestNews
            WHERE RowNumber = 1
              AND Id = @Id
            """;

        var results = await QueryAdminAsync(sql, new { Id = id }, cancellationToken);
        return results.FirstOrDefault();
    }

    public async Task<int> CreateDraftAsync(AdminNewsDraft draft, string editorEmail, CancellationToken cancellationToken = default)
    {
        const string nextIdSql = "SELECT ISNULL(MAX(NEWS_ID), 0) + 1 FROM NEWS_T";

        const string insertSql = """
            INSERT INTO NEWS_T (
                NEWS_ID,
                TITLE,
                EXCERPT,
                ARTICLE,
                [DATE],
                SOURCE_URL,
                DISPLAY,
                SLUG,
                CREATED_AT,
                UPDATED_AT,
                EDITOR_EMAIL,
                USER_ID,
                TYPE,
                QUEEN_ONLINE)
            VALUES (
                @Id,
                @Title,
                @Excerpt,
                @Article,
                @PublishedAt,
                @SourceUrl,
                0,
                @Slug,
                @Timestamp,
                @Timestamp,
                @EditorEmail,
                NULL,
                0,
                0)
            """;

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var nextId = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(nextIdSql, cancellationToken: cancellationToken));

        var slug = NewsSlug.Resolve(draft.Title, draft.Slug);
        var timestamp = DateTime.UtcNow;

        await connection.ExecuteAsync(new CommandDefinition(
            insertSql,
            new
            {
                Id = nextId,
                draft.Title,
                draft.Excerpt,
                Article = draft.Body,
                draft.PublishedAt,
                draft.SourceUrl,
                Slug = slug,
                Timestamp = timestamp,
                EditorEmail = editorEmail
            },
            cancellationToken: cancellationToken));

        return nextId;
    }

    public async Task UpdateAsync(int id, AdminNewsDraft draft, string editorEmail, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE NEWS_T
            SET
                TITLE = @Title,
                EXCERPT = @Excerpt,
                ARTICLE = @Article,
                [DATE] = @PublishedAt,
                SOURCE_URL = @SourceUrl,
                SLUG = @Slug,
                UPDATED_AT = @UpdatedAt,
                EDITOR_EMAIL = @EditorEmail
            WHERE NEWS_ID = @Id
            """;

        await using var connection = new SqlConnection(connectionString);
        var command = new CommandDefinition(
            sql,
            new
            {
                Id = id,
                draft.Title,
                draft.Excerpt,
                Article = draft.Body,
                draft.PublishedAt,
                draft.SourceUrl,
                Slug = NewsSlug.Resolve(draft.Title, draft.Slug),
                UpdatedAt = DateTime.UtcNow,
                EditorEmail = editorEmail
            },
            cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);
    }

    public async Task PublishAsync(int id, string editorEmail, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE NEWS_T
            SET
                DISPLAY = 1,
                UPDATED_AT = @UpdatedAt,
                EDITOR_EMAIL = @EditorEmail
            WHERE NEWS_ID = @Id
            """;

        await using var connection = new SqlConnection(connectionString);
        var command = new CommandDefinition(
            sql,
            new { Id = id, UpdatedAt = DateTime.UtcNow, EditorEmail = editorEmail },
            cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);
    }

    public async Task UnpublishAsync(int id, string editorEmail, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE NEWS_T
            SET
                DISPLAY = 0,
                UPDATED_AT = @UpdatedAt,
                EDITOR_EMAIL = @EditorEmail
            WHERE NEWS_ID = @Id
            """;

        await using var connection = new SqlConnection(connectionString);
        var command = new CommandDefinition(
            sql,
            new { Id = id, UpdatedAt = DateTime.UtcNow, EditorEmail = editorEmail },
            cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);
    }

    public async Task DeleteAsync(int id, string editorEmail, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM NEWS_T WHERE NEWS_ID = @Id";

        await using var connection = new SqlConnection(connectionString);
        var command = new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);
    }

    public async Task<bool> IsSlugInUseAsync(string slug, int? excludeNewsId = null, CancellationToken cancellationToken = default)
    {
        var articles = await GetAllAsync(cancellationToken);
        var normalized = NewsSlug.Slugify(slug);

        return articles.Any(article =>
            article.Id != excludeNewsId
            && string.Equals(NewsSlug.ResolveForArticle(article), normalized, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<IReadOnlyList<AdminNewsArticle>> QueryAdminAsync(
        string sql,
        object parameters,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        var command = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<AdminNewsRow>(command);

        return rows
            .Select(row => new AdminNewsArticle(
                row.Id,
                row.Title,
                row.Slug ?? string.Empty,
                row.Excerpt,
                row.Body,
                row.PublishedAt,
                row.SourceUrl,
                row.IsPublished,
                row.CreatedAt,
                row.UpdatedAt,
                row.EditorEmail))
            .ToList();
    }

    private sealed class AdminNewsRow
    {
        public int Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public string? Slug { get; init; }
        public string Excerpt { get; init; } = string.Empty;
        public string Body { get; init; } = string.Empty;
        public DateTime PublishedAt { get; init; }
        public string? SourceUrl { get; init; }
        public bool IsPublished { get; init; }
        public DateTime? CreatedAt { get; init; }
        public DateTime? UpdatedAt { get; init; }
        public string? EditorEmail { get; init; }
    }
}