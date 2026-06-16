using Microsoft.EntityFrameworkCore;
using QueenZone.Data.Entities;

namespace QueenZone.Data;

public sealed class EfAdminNewsRepository(QueenZoneDbContext dbContext) : IAdminNewsRepository
{
    private const string LatestNewsSql = """
        WITH LatestNews AS (
            SELECT
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
                QUEEN_ONLINE,
                ROW_NUMBER() OVER (PARTITION BY NEWS_ID ORDER BY [DATE] DESC, NEWS_ID DESC) AS RowNumber
            FROM NEWS_T
        )
        SELECT
            NEWS_ID AS NewsId,
            TITLE AS Title,
            ISNULL(EXCERPT, '') AS Excerpt,
            ISNULL(ARTICLE, '') AS Body,
            [DATE] AS PublishedAt,
            SOURCE_URL AS SourceUrl,
            CAST(CASE WHEN DISPLAY = 1 THEN 1 ELSE 0 END AS bit) AS IsPublished,
            SLUG AS Slug,
            CREATED_AT AS CreatedAt,
            UPDATED_AT AS UpdatedAt,
            EDITOR_EMAIL AS EditorEmail,
            USER_ID AS UserId,
            TYPE AS Type,
            QUEEN_ONLINE AS QueenOnline
        FROM LatestNews
        WHERE RowNumber = 1
        """;

    public async Task<IReadOnlyList<AdminNewsArticle>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.NewsRows
            .FromSqlRaw($"{LatestNewsSql} ORDER BY PublishedAt DESC, NewsId DESC")
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return rows.Select(NewsTableRowMapper.ToAdminArticle).ToList();
    }

    public async Task<AdminNewsArticle?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var row = await dbContext.NewsRows
            .FromSqlRaw($"{LatestNewsSql} AND NEWS_ID = {{0}}", id)
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);

        return row is null ? null : NewsTableRowMapper.ToAdminArticle(row);
    }

    public async Task<int> CreateDraftAsync(AdminNewsDraft draft, string editorEmail, CancellationToken cancellationToken = default)
    {
        var nextId = (await dbContext.NewsRows.MaxAsync(row => (int?)row.NewsId, cancellationToken) ?? 0) + 1;
        var timestamp = DateTime.UtcNow;

        var row = new NewsTableRow
        {
            NewsId = nextId,
            Title = draft.Title,
            Excerpt = draft.Excerpt,
            Body = draft.Body,
            PublishedAt = draft.PublishedAt,
            SourceUrl = draft.SourceUrl,
            IsPublished = false,
            Slug = NewsSlug.Resolve(draft.Title, draft.Slug),
            CreatedAt = timestamp,
            UpdatedAt = timestamp,
            EditorEmail = editorEmail,
            Type = 0,
            QueenOnline = 0
        };

        dbContext.NewsRows.Add(row);
        await dbContext.SaveChangesAsync(cancellationToken);
        return nextId;
    }

    public async Task UpdateAsync(int id, AdminNewsDraft draft, string editorEmail, CancellationToken cancellationToken = default)
    {
        var updated = await dbContext.NewsRows
            .Where(row => row.NewsId == id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(row => row.Title, draft.Title)
                    .SetProperty(row => row.Excerpt, draft.Excerpt)
                    .SetProperty(row => row.Body, draft.Body)
                    .SetProperty(row => row.PublishedAt, draft.PublishedAt)
                    .SetProperty(row => row.SourceUrl, draft.SourceUrl)
                    .SetProperty(row => row.Slug, NewsSlug.Resolve(draft.Title, draft.Slug))
                    .SetProperty(row => row.UpdatedAt, DateTime.UtcNow)
                    .SetProperty(row => row.EditorEmail, editorEmail),
                cancellationToken);

        if (updated == 0)
        {
            throw new InvalidOperationException($"News article {id} was not found.");
        }
    }

    public async Task PublishAsync(int id, string editorEmail, CancellationToken cancellationToken = default)
    {
        var updated = await dbContext.NewsRows
            .Where(row => row.NewsId == id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(row => row.IsPublished, true)
                    .SetProperty(row => row.UpdatedAt, DateTime.UtcNow)
                    .SetProperty(row => row.EditorEmail, editorEmail),
                cancellationToken);

        if (updated == 0)
        {
            throw new InvalidOperationException($"News article {id} was not found.");
        }
    }

    public async Task UnpublishAsync(int id, string editorEmail, CancellationToken cancellationToken = default)
    {
        var updated = await dbContext.NewsRows
            .Where(row => row.NewsId == id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(row => row.IsPublished, false)
                    .SetProperty(row => row.UpdatedAt, DateTime.UtcNow)
                    .SetProperty(row => row.EditorEmail, editorEmail),
                cancellationToken);

        if (updated == 0)
        {
            throw new InvalidOperationException($"News article {id} was not found.");
        }
    }

    public async Task DeleteAsync(int id, string editorEmail, CancellationToken cancellationToken = default)
    {
        var deleted = await dbContext.NewsRows
            .Where(row => row.NewsId == id)
            .ExecuteDeleteAsync(cancellationToken);

        if (deleted == 0)
        {
            throw new InvalidOperationException($"News article {id} was not found.");
        }
    }

    public async Task<bool> IsSlugInUseAsync(string slug, int? excludeNewsId = null, CancellationToken cancellationToken = default)
    {
        var articles = await GetAllAsync(cancellationToken);
        var normalized = NewsSlug.Slugify(slug);

        return articles.Any(article =>
            article.Id != excludeNewsId
            && string.Equals(NewsSlug.ResolveForArticle(article), normalized, StringComparison.OrdinalIgnoreCase));
    }
}