using QueenZone.Data;

namespace QueenZone.Web.Tests;

internal sealed class FixedNewsRepository : INewsRepository
{
    private readonly IReadOnlyList<NewsItem> publishedItems;

    public FixedNewsRepository(IEnumerable<NewsItem> rawItems)
    {
        publishedItems = NewsItemOrdering.ByCreatedDateDescending(
            rawItems
                .Where(item => item.IsPublished)
                .GroupBy(item => item.Id)
                .Select(group => NewsItemOrdering.ByCreatedDateDescending(group).First()));
    }

    public Task<IReadOnlyList<NewsItem>> GetLatestAsync(int count, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<NewsItem>>(publishedItems.Take(count).ToList());

    public Task<IReadOnlyList<NewsItem>> GetArchivePageAsync(
        int page,
        int pageSize,
        NewsArchiveFilter filter = default,
        CancellationToken cancellationToken = default)
    {
        var filtered = NewsArchiveFiltering.Apply(publishedItems, filter);
        var skip = Math.Max(page - 1, 0) * pageSize;
        return Task.FromResult<IReadOnlyList<NewsItem>>(filtered.Skip(skip).Take(pageSize).ToList());
    }

    public Task<int> GetPublishedCountAsync(NewsArchiveFilter filter = default, CancellationToken cancellationToken = default) =>
        Task.FromResult(NewsArchiveFiltering.Apply(publishedItems, filter).Count);

    public Task<NewsArchiveYearRange> GetArchiveYearRangeAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(NewsArchiveYearRanges.Compute(publishedItems));

    public Task<NewsItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(publishedItems.SingleOrDefault(item => item.Id == id));

    public Task<IReadOnlyList<NewsItem>> GetByIdsAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return Task.FromResult<IReadOnlyList<NewsItem>>([]);
        }

        var idSet = ids as ISet<int> ?? ids.ToHashSet();
        return Task.FromResult<IReadOnlyList<NewsItem>>(
            publishedItems.Where(item => idSet.Contains(item.Id)).ToList());
    }

    public Task<IReadOnlyList<SitemapContentEntry>> GetPublishedSitemapEntriesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SitemapContentEntry>>(publishedItems
            .Select(item => new SitemapContentEntry(item.Id, item.Title, item.PublishedAt, item.Slug))
            .ToList());

    public Task<NewsSearchPage> SearchAsync(string query, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Task.FromResult(new NewsSearchPage([], 0, page, pageSize));
        }

        var term = query.Trim();
        var matches = publishedItems
            .Where(item =>
                item.Title.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                item.Excerpt.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                item.Body.Contains(term, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var normalizedPage = Math.Max(page, 1);
        var skip = (normalizedPage - 1) * pageSize;
        var items = matches.Skip(skip).Take(pageSize).ToList();

        return Task.FromResult(new NewsSearchPage(items, matches.Count, normalizedPage, pageSize));
    }
}
