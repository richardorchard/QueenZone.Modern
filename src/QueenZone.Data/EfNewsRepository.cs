using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace QueenZone.Data;

/// <summary>
/// Public news archive reads against legacy <c>NEWS_T</c> latest-row projections via EF Core.
/// </summary>
public sealed class EfNewsRepository : INewsRepository
{
    private readonly QueenZoneDbContext dbContext;
    private readonly Func<int, string> latestSql;
    private readonly string countSql;
    private readonly Func<int, int, string> archivePageSql;
    private readonly Func<int, string> byIdSql;
    private readonly string sitemapSql;

    [ExcludeFromCodeCoverage]
    public EfNewsRepository(QueenZoneDbContext dbContext)
    {
        this.dbContext = dbContext;
        var connectionString = dbContext.Database.GetConnectionString()
            ?? throw new InvalidOperationException("QueenZone legacy database connection string is not configured.");
        var publishedNewsCte = PublishedNewsQuery.BuildPublishedNewsCte(
            LegacyNewsSchema.HasSlugColumn(connectionString));
        (latestSql, countSql, archivePageSql, byIdSql, sitemapSql) =
            EfProductionSql.CreateNewsQueries(publishedNewsCte);
    }

    internal EfNewsRepository(
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

    public async Task<IReadOnlyList<NewsItem>> GetLatestAsync(int count, CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.Database
            .SqlQueryRaw<NewsRow>(latestSql(count))
            .ToListAsync(cancellationToken);
        return rows.Select(Map).ToList();
    }

    public async Task<int> GetPublishedCountAsync(CancellationToken cancellationToken = default)
    {
        // CTE SQL is non-composable; materialize fully instead of FirstAsync (which tries to compose).
        var values = await dbContext.Database
            .SqlQueryRaw<int>(countSql)
            .ToListAsync(cancellationToken);
        return values.FirstOrDefault();
    }

    public async Task<IReadOnlyList<NewsItem>> GetArchivePageAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var offset = Math.Max(page - 1, 0) * pageSize;
        var rows = await dbContext.Database
            .SqlQueryRaw<NewsRow>(archivePageSql(offset, pageSize))
            .ToListAsync(cancellationToken);
        return rows.Select(Map).ToList();
    }

    public async Task<NewsItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.Database
            .SqlQueryRaw<NewsRow>(byIdSql(id))
            .ToListAsync(cancellationToken);
        var row = rows.FirstOrDefault();
        return row is null ? null : Map(row);
    }

    public async Task<IReadOnlyList<SitemapContentEntry>> GetPublishedSitemapEntriesAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.Database
            .SqlQueryRaw<SitemapContentEntry>(sitemapSql)
            .ToListAsync(cancellationToken);

    private static NewsItem Map(NewsRow row) =>
        new(
            row.Id,
            row.Title,
            row.Excerpt,
            row.Body,
            row.PublishedAt,
            row.SourceUrl,
            row.IsPublished,
            row.Slug);

    internal sealed class NewsRow
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Excerpt { get; set; } = string.Empty;

        public string Body { get; set; } = string.Empty;

        public DateTime PublishedAt { get; set; }

        public string? SourceUrl { get; set; }

        public bool IsPublished { get; set; }

        public string? Slug { get; set; }
    }
}
