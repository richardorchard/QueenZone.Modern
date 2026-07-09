using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace QueenZone.Data;

/// <summary>
/// Public article archive reads against legacy tables via EF Core SQL queries.
/// </summary>
public sealed class EfArticlesRepository : IArticlesRepository
{
    private readonly QueenZoneDbContext dbContext;
    private readonly Func<int, string> latestSql;
    private readonly string countSql;
    private readonly Func<int, int, string> archivePageSql;
    private readonly Func<int, string> byIdSql;
    private readonly string sitemapSql;

    [ExcludeFromCodeCoverage]
    public EfArticlesRepository(QueenZoneDbContext dbContext)
    {
        this.dbContext = dbContext;
        (latestSql, countSql, archivePageSql, byIdSql, sitemapSql) = EfProductionSql.CreateArticlesQueries();
    }

    internal EfArticlesRepository(
        QueenZoneDbContext dbContext,
        Func<int, string> latestSql,
        string countSql,
        Func<int, int, string> archivePageSql,
        Func<int, string> byIdSql,
        string sitemapSql)
    {
        this.dbContext = dbContext;
        this.latestSql = latestSql;
        this.countSql = countSql;
        this.archivePageSql = archivePageSql;
        this.byIdSql = byIdSql;
        this.sitemapSql = sitemapSql;
    }

    public async Task<IReadOnlyList<ArticleItem>> GetLatestAsync(int count, CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.Database
            .SqlQueryRaw<ArticleRow>(latestSql(count))
            .ToListAsync(cancellationToken);
        return rows.Select(Map).ToList();
    }

    public async Task<int> GetPublishedCountAsync(CancellationToken cancellationToken = default)
    {
        // Avoid FirstAsync composition over raw SQL (can fail for some SQL Server shapes).
        var values = await dbContext.Database
            .SqlQueryRaw<int>(countSql)
            .ToListAsync(cancellationToken);
        return values.FirstOrDefault();
    }

    public async Task<IReadOnlyList<ArticleItem>> GetArchivePageAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var offset = Math.Max(page - 1, 0) * pageSize;
        var rows = await dbContext.Database
            .SqlQueryRaw<ArticleRow>(archivePageSql(offset, pageSize))
            .ToListAsync(cancellationToken);
        return rows.Select(Map).ToList();
    }

    public async Task<ArticleItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.Database
            .SqlQueryRaw<ArticleRow>(byIdSql(id))
            .ToListAsync(cancellationToken);
        var row = rows.FirstOrDefault();
        return row is null ? null : Map(row);
    }

    public async Task<IReadOnlyList<SitemapContentEntry>> GetPublishedSitemapEntriesAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.Database
            .SqlQueryRaw<SitemapContentEntry>(sitemapSql)
            .ToListAsync(cancellationToken);

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
