namespace QueenZone.Data;

public sealed class SampleNewsRepository : INewsRepository
{
    private static readonly IReadOnlyList<NewsItem> Items = BuildItems();

    private static IReadOnlyList<NewsItem> PublishedItems =>
        Items.Where(item => item.IsPublished).ToList();

    public Task<IReadOnlyList<NewsItem>> GetLatestAsync(int count, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<NewsItem>>(PublishedItems.Take(count).ToList());

    public Task<IReadOnlyList<NewsItem>> GetArchivePageAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var skip = Math.Max(page - 1, 0) * pageSize;
        return Task.FromResult<IReadOnlyList<NewsItem>>(PublishedItems.Skip(skip).Take(pageSize).ToList());
    }

    public Task<int> GetPublishedCountAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(PublishedItems.Count);

    public Task<NewsItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(
            NewsItemOrdering.ByCreatedDateDescending(
                    PublishedItems.Where(item => item.Id == id))
                .FirstOrDefault());

    public Task<IReadOnlyList<SitemapContentEntry>> GetPublishedSitemapEntriesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SitemapContentEntry>>(PublishedItems
            .Select(item => new SitemapContentEntry(item.Id, item.Title, item.PublishedAt, item.Slug))
            .ToList());

    private static IReadOnlyList<NewsItem> BuildItems()
    {
        var items = new List<NewsItem>
        {
            new(
                1003,
                "QueenZone modernisation begins",
                "The first local vertical slice is now running from the new ASP.NET Core application.",
                "This placeholder item keeps the local site useful until a restored legacy SQL Server connection is configured.",
                new DateTime(2026, 6, 11, 9, 0, 0, DateTimeKind.Utc),
                null,
                true),
            new(
                1002,
                "Legacy news archive mapped",
                "The first migration target is NEWS_T, with clean canonical routes for the modern archive.",
                "News gives the rebuild a narrow, valuable slice through data access, routing, page rendering, search-friendly URLs, and deployment.",
                new DateTime(2026, 6, 10, 9, 0, 0, DateTimeKind.Utc),
                null,
                true),
            new(
                1001,
                "Read-only launch strategy accepted",
                "The first public release will focus on preserving archive content before restoring interactive features.",
                "Accounts, posting, uploads, private messages, and administration are intentionally out of scope for the first release.",
                new DateTime(2026, 6, 9, 9, 0, 0, DateTimeKind.Utc),
                null,
                true),
            new(
                9001,
                "Hidden moderation draft",
                "This record should never appear in public archive output.",
                "Moderated content remains in the legacy database but is excluded from the modern read-only archive.",
                new DateTime(2026, 6, 12, 9, 0, 0, DateTimeKind.Utc),
                null,
                false)
        };

        for (var id = 1004; id <= 1022; id++)
        {
            var dayOffset = 1022 - id;
            items.Add(new NewsItem(
                id,
                $"Archive sample article {id}",
                $"Excerpt for archive sample article {id}.",
                $"Body for archive sample article {id}.",
                new DateTime(2026, 6, 8, 9, 0, 0, DateTimeKind.Utc).AddDays(-dayOffset),
                null,
                true));
        }

        return NewsItemOrdering.ByCreatedDateDescending(items);
    }
}