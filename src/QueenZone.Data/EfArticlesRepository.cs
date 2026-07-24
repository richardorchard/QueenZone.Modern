using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace QueenZone.Data;

/// <summary>
/// Public article archive reads against legacy tables via EF Core SQL queries.
/// </summary>
public sealed class EfArticlesRepository : IArticlesRepository
{
    private const int MaxPageSize = 100;
    private const int MaxLatestCount = 100;

    private readonly QueenZoneDbContext dbContext;
    private readonly string latestSql;
    private readonly string countSql;
    private readonly string archivePageSql;
    private readonly string byIdSql;
    private readonly string sitemapSql;

    [ExcludeFromCodeCoverage]
    public EfArticlesRepository(QueenZoneDbContext dbContext)
    {
        this.dbContext = dbContext;
        (latestSql, countSql, archivePageSql, byIdSql, sitemapSql) = EfProductionSql.CreateArticlesQueries();
    }

    /// <summary>
    /// Test constructor: SQL templates must use EF <c>{0}</c>/<c>{1}</c> placeholders for
    /// dynamic ints (same as production <see cref="EfProductionSql"/>).
    /// </summary>
    internal EfArticlesRepository(
        QueenZoneDbContext dbContext,
        string latestSql,
        string countSql,
        string archivePageSql,
        string byIdSql,
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
        var take = Math.Clamp(count, 1, MaxLatestCount);
        var rows = await dbContext.Database
            .SqlQueryRaw<ArticleRow>(latestSql, take)
            .ToListAsync(cancellationToken);
        return rows.Select(MapList).ToList();
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
        var normalizedPage = Math.Max(page, 1);
        var take = Math.Clamp(pageSize, 1, MaxPageSize);
        var offset = (normalizedPage - 1) * take;
        var rows = await dbContext.Database
            .SqlQueryRaw<ArticleRow>(archivePageSql, offset, take)
            .ToListAsync(cancellationToken);
        return rows.Select(MapList).ToList();
    }

    public async Task<ArticleItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.Database
            .SqlQueryRaw<ArticleRow>(byIdSql, id)
            .ToListAsync(cancellationToken);
        var row = rows.FirstOrDefault();
        return row is null ? null : MapDetail(row);
    }

    public async Task<IReadOnlyList<SitemapContentEntry>> GetPublishedSitemapEntriesAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.Database
            .SqlQueryRaw<SitemapContentEntry>(sitemapSql)
            .ToListAsync(cancellationToken);

    /// <summary>
    /// List/archive mapping: derive excerpt from optional body preview; never keep full body on list items.
    /// Body may be empty when the list SQL omits it entirely.
    /// </summary>
    private static ArticleItem MapList(ArticleRow row) =>
        new(
            row.Id,
            row.Title,
            LegacyArticleText.GetExcerpt(row.Body),
            string.Empty,
            row.PublishedAt,
            row.Source,
            row.CategoryName,
            row.IsPublished);

    private static ArticleItem MapDetail(ArticleRow row) =>
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
