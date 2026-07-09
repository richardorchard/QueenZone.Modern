using Microsoft.EntityFrameworkCore;

namespace QueenZone.Data;

/// <summary>
/// Public news archive reads against legacy <c>NEWS_T</c> latest-row projections via EF Core.
/// </summary>
public sealed class EfNewsRepository : INewsRepository
{
    private readonly QueenZoneDbContext dbContext;
    private readonly string publishedNewsCte;

    public EfNewsRepository(QueenZoneDbContext dbContext)
    {
        this.dbContext = dbContext;
        var connectionString = dbContext.Database.GetConnectionString()
            ?? throw new InvalidOperationException("QueenZone legacy database connection string is not configured.");
        publishedNewsCte = LegacyNewsSchema.BuildPublishedNewsCte(
            LegacyNewsSchema.HasSlugColumn(connectionString));
    }

    internal EfNewsRepository(QueenZoneDbContext dbContext, string publishedNewsCte)
    {
        this.dbContext = dbContext;
        this.publishedNewsCte = publishedNewsCte;
    }

    public async Task<IReadOnlyList<NewsItem>> GetLatestAsync(int count, CancellationToken cancellationToken = default)
    {
        var sql = publishedNewsCte + """

            SELECT TOP ({0})
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
            ORDER BY PublishedAt DESC, Id DESC
            """;

        var rows = await dbContext.Database
            .SqlQueryRaw<NewsRow>(sql, count)
            .ToListAsync(cancellationToken);
        return rows.Select(Map).ToList();
    }

    public async Task<int> GetPublishedCountAsync(CancellationToken cancellationToken = default)
    {
        var sql = publishedNewsCte + """

            SELECT COUNT(*) AS Value
            FROM PublishedNews
            WHERE RowNumber = 1
            """;

        return await dbContext.Database
            .SqlQueryRaw<int>(sql)
            .FirstAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NewsItem>> GetArchivePageAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var offset = Math.Max(page - 1, 0) * pageSize;
        var sql = publishedNewsCte + """

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
            ORDER BY PublishedAt DESC, Id DESC
            OFFSET {0} ROWS FETCH NEXT {1} ROWS ONLY
            """;

        var rows = await dbContext.Database
            .SqlQueryRaw<NewsRow>(sql, offset, pageSize)
            .ToListAsync(cancellationToken);
        return rows.Select(Map).ToList();
    }

    public async Task<NewsItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var sql = publishedNewsCte + """

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
              AND Id = {0}
            """;

        var rows = await dbContext.Database
            .SqlQueryRaw<NewsRow>(sql, id)
            .ToListAsync(cancellationToken);
        var row = rows.FirstOrDefault();
        return row is null ? null : Map(row);
    }

    public async Task<IReadOnlyList<SitemapContentEntry>> GetPublishedSitemapEntriesAsync(
        CancellationToken cancellationToken = default)
    {
        var sql = publishedNewsCte + """

            SELECT
                Id,
                Title,
                PublishedAt,
               Slug
            FROM PublishedNews
            WHERE RowNumber = 1
            ORDER BY PublishedAt DESC, Id DESC
            """;

        return await dbContext.Database
            .SqlQueryRaw<SitemapContentEntry>(sql)
            .ToListAsync(cancellationToken);
    }

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
