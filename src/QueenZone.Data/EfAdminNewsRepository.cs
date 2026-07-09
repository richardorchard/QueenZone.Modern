using Microsoft.EntityFrameworkCore;
using QueenZone.Data.Entities;

namespace QueenZone.Data;

public sealed class EfAdminNewsRepository : IAdminNewsRepository
{
    private readonly QueenZoneDbContext dbContext;
    private readonly string latestNewsSql;
    private readonly string latestNewsCountSql;
    private readonly string connectionString;

    public EfAdminNewsRepository(QueenZoneDbContext dbContext)
        : this(dbContext, latestNewsSqlOverride: null)
    {
    }

    internal EfAdminNewsRepository(QueenZoneDbContext dbContext, string? latestNewsSqlOverride)
    {
        this.dbContext = dbContext;
        connectionString = dbContext.Database.GetConnectionString()
            ?? throw new InvalidOperationException("QueenZone legacy database connection string is not configured.");
        if (latestNewsSqlOverride is not null)
        {
            latestNewsSql = latestNewsSqlOverride;
            latestNewsCountSql = "SELECT COUNT(*) AS [Value] FROM NEWS_T";
        }
        else
        {
            var columns = LegacyNewsSchema.GetNewsColumnAvailability(connectionString);
            latestNewsSql = LegacyNewsSchema.BuildAdminLatestNewsSql(columns);
            latestNewsCountSql = LegacyNewsSchema.BuildAdminLatestNewsCountSql(columns);
        }
    }

    public async Task<IReadOnlyList<AdminNewsArticle>> GetAllAsync(CancellationToken cancellationToken = default)
    {
#pragma warning disable EF1003 // SQL is generated from fixed schema-detection branches, not user input.
        var rows = await dbContext.NewsRows
            .FromSqlRaw(latestNewsSql + " ORDER BY PublishedAt DESC, NewsId DESC")
            .AsNoTracking()
            .ToListAsync(cancellationToken);
#pragma warning restore EF1003

        return rows.Select(NewsTableRowMapper.ToAdminArticle).ToList();
    }

    public async Task<AdminNewsArticlePage> GetPageAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var normalizedPage = Math.Max(page, 1);
        var normalizedPageSize = Math.Max(pageSize, 1);
        var offset = (normalizedPage - 1) * normalizedPageSize;

        var totalCount = await GetAdminNewsTotalCountAsync(cancellationToken);

        var pagingSuffix = IsSqliteDatabase()
            ? " ORDER BY PublishedAt DESC, NewsId DESC LIMIT {1} OFFSET {0}"
            : " ORDER BY PublishedAt DESC, NewsId DESC OFFSET {0} ROWS FETCH NEXT {1} ROWS ONLY";

#pragma warning disable EF1003 // SQL is generated from fixed schema-detection branches, not user input.
        var rows = await dbContext.NewsRows
            .FromSqlRaw(
                latestNewsSql + pagingSuffix,
                offset,
                normalizedPageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
#pragma warning restore EF1003

        return new AdminNewsArticlePage(
            rows.Select(NewsTableRowMapper.ToAdminArticle).ToList(),
            totalCount,
            normalizedPage,
            normalizedPageSize);
    }

    public async Task<AdminNewsArticle?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
#pragma warning disable EF1003 // SQL is generated from fixed schema-detection branches, not user input.
        // Materialize on the client: EF cannot compose SingleOrDefault over this CTE-based SQL.
        var rows = await dbContext.NewsRows
            .FromSqlRaw(latestNewsSql + " AND NEWS_ID = {0}", id)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
#pragma warning restore EF1003

        var row = rows.FirstOrDefault();
        return row is null ? null : NewsTableRowMapper.ToAdminArticle(row);
    }

    public async Task<int> CreateDraftAsync(AdminNewsDraft draft, string editorEmail, CancellationToken cancellationToken = default)
    {
        var timestamp = DateTime.UtcNow;

        var row = new NewsTableRow
        {
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
        return row.NewsId;
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

    private async Task<int> GetAdminNewsTotalCountAsync(CancellationToken cancellationToken) =>
        await dbContext.Database
            .SqlQueryRaw<int>(latestNewsCountSql)
            .FirstAsync(cancellationToken);

    private bool IsSqliteDatabase() =>
        string.Equals(
            dbContext.Database.ProviderName,
            "Microsoft.EntityFrameworkCore.Sqlite",
            StringComparison.Ordinal);
}
