using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace QueenZone.Data;

/// <summary>
/// Public news archive reads against legacy <c>NEWS_T</c> latest-row projections via EF Core.
/// </summary>
public sealed class EfNewsRepository : INewsRepository
{
    private const int MaxPageSize = 100;
    private const int MaxLatestCount = 100;

    private readonly QueenZoneDbContext dbContext;
    private readonly string latestSql;
    private readonly string countSql;
    private readonly string archivePageSql;
    private readonly string byIdSql;
    private readonly string sitemapSql;

    // SQLite-only: LIKE-based fallback for deterministic tests.
    // On SQL Server the SearchAsync path uses dbo.NEWS_T_SearchPublished instead.
    private readonly string sqliteLikeSearchSql;
    private readonly string sqliteLikeSearchCountSql;

    [ExcludeFromCodeCoverage]
    public EfNewsRepository(QueenZoneDbContext dbContext)
    {
        this.dbContext = dbContext;
        var connectionString = dbContext.Database.GetConnectionString()
            ?? throw new InvalidOperationException("QueenZone legacy database connection string is not configured.");
        var includeSlug = LegacyNewsSchema.HasSlugColumn(connectionString);
        // List/count/sitemap omit ARTICLE; detail still projects full body.
        var listCte = PublishedNewsQuery.BuildPublishedNewsCte(includeSlug, includeBody: false);
        var detailCte = PublishedNewsQuery.BuildPublishedNewsCte(includeSlug, includeBody: true);
        (latestSql, countSql, archivePageSql, byIdSql, sitemapSql) =
            EfProductionSql.CreateNewsQueries(listCte, detailCte);
        // SQLite fallback: body-inclusive CTE for LIKE matching (not used on SQL Server).
        var searchCte = PublishedNewsQuery.BuildPublishedNewsCte(includeSlug, includeBody: true);
        (sqliteLikeSearchSql, sqliteLikeSearchCountSql) =
            EfProductionSql.CreateNewsSqliteLikeSearchQueries(searchCte);
    }

    /// <summary>
    /// Test constructor: accepts SQLite-compatible LIKE search SQL for the search fallback path.
    /// SQL templates must use EF <c>{0}</c>/<c>{1}</c>/<c>{2}</c> placeholders.
    /// </summary>
    internal EfNewsRepository(
        QueenZoneDbContext dbContext,
        string latestSql,
        string countSql,
        string archivePageSql,
        string byIdSql,
        string sitemapSql,
        string sqliteLikeSearchSql = "",
        string sqliteLikeSearchCountSql = "")
    {
        this.dbContext = dbContext;
        this.latestSql = latestSql;
        this.countSql = countSql;
        this.archivePageSql = archivePageSql;
        this.byIdSql = byIdSql;
        this.sitemapSql = sitemapSql;
        this.sqliteLikeSearchSql = sqliteLikeSearchSql;
        this.sqliteLikeSearchCountSql = sqliteLikeSearchCountSql;
    }

    public async Task<IReadOnlyList<NewsItem>> GetLatestAsync(int count, CancellationToken cancellationToken = default)
    {
        var take = Math.Clamp(count, 1, MaxLatestCount);
        var rows = await dbContext.Database
            .SqlQueryRaw<NewsRow>(latestSql, take)
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
        var normalizedPage = Math.Max(page, 1);
        var take = Math.Clamp(pageSize, 1, MaxPageSize);
        var offset = (normalizedPage - 1) * take;
        var rows = await dbContext.Database
            .SqlQueryRaw<NewsRow>(archivePageSql, offset, take)
            .ToListAsync(cancellationToken);
        return rows.Select(Map).ToList();
    }

    public async Task<NewsItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.Database
            .SqlQueryRaw<NewsRow>(byIdSql, id)
            .ToListAsync(cancellationToken);
        var row = rows.FirstOrDefault();
        return row is null ? null : Map(row);
    }

    public async Task<IReadOnlyList<SitemapContentEntry>> GetPublishedSitemapEntriesAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.Database
            .SqlQueryRaw<SitemapContentEntry>(sitemapSql)
            .ToListAsync(cancellationToken);

    public async Task<NewsSearchPage> SearchAsync(
        string query,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new NewsSearchPage([], 0, page, pageSize);
        }

        if (IsSqliteDatabase())
        {
            return await ExecuteSearchWithLikeAsync(query.Trim(), page, pageSize, cancellationToken);
        }

        return await ExecuteSearchWithFtsAsync(query.Trim(), page, pageSize, cancellationToken);
    }

    [ExcludeFromCodeCoverage]
    private async Task<NewsSearchPage> ExecuteSearchWithFtsAsync(
        string query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var normalizedPage = Math.Max(page, 1);
        var take = Math.Clamp(pageSize, 1, MaxPageSize);
        var offset = (normalizedPage - 1) * take;
        var totalRecords = EfSql.OutputInt("@TotalRecords");

        var rows = await EfSql.QueryProcAsync<NewsRow>(
            dbContext,
            "NEWS_T_SearchPublished",
            command =>
            {
                command.Parameters.Add(EfSql.Input("@Query", query));
                command.Parameters.Add(EfSql.Input("@Offset", offset));
                command.Parameters.Add(EfSql.Input("@PageSize", take));
                command.Parameters.Add(totalRecords);
            },
            cancellationToken: cancellationToken);

        return new NewsSearchPage(
            rows.Select(Map).ToList(),
            EfSql.GetNullableInt(totalRecords) ?? 0,
            normalizedPage,
            take);
    }

    private async Task<NewsSearchPage> ExecuteSearchWithLikeAsync(
        string query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var normalizedPage = Math.Max(page, 1);
        var take = Math.Clamp(pageSize, 1, MaxPageSize);
        var offset = (normalizedPage - 1) * take;
        var likePattern = $"%{query}%";

        var countValues = await dbContext.Database
            .SqlQueryRaw<int>(sqliteLikeSearchCountSql, likePattern)
            .ToListAsync(cancellationToken);
        var totalCount = countValues.FirstOrDefault();

        if (totalCount == 0)
        {
            return new NewsSearchPage([], 0, normalizedPage, take);
        }

        var rows = await dbContext.Database
            .SqlQueryRaw<NewsRow>(sqliteLikeSearchSql, likePattern, offset, take)
            .ToListAsync(cancellationToken);

        return new NewsSearchPage(rows.Select(Map).ToList(), totalCount, normalizedPage, take);
    }

    private bool IsSqliteDatabase() =>
        string.Equals(
            dbContext.Database.ProviderName,
            "Microsoft.EntityFrameworkCore.Sqlite",
            StringComparison.Ordinal);

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
