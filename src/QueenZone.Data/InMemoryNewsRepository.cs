namespace QueenZone.Data;

public sealed class InMemoryNewsRepository(SharedNewsStore store) : INewsRepository
{
    public Task<IReadOnlyList<NewsItem>> GetLatestAsync(int count, CancellationToken cancellationToken = default)
    {
        var published = store.GetPublishedNewsItems();
        return Task.FromResult<IReadOnlyList<NewsItem>>(published.Take(count).ToList());
    }

    public Task<IReadOnlyList<NewsItem>> GetArchivePageAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var published = store.GetPublishedNewsItems();
        var skip = Math.Max(page - 1, 0) * pageSize;
        return Task.FromResult<IReadOnlyList<NewsItem>>(published.Skip(skip).Take(pageSize).ToList());
    }

    public Task<int> GetPublishedCountAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(store.GetPublishedNewsItems().Count);

    public Task<NewsItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(store.GetPublishedNewsItems().SingleOrDefault(item => item.Id == id));

    public Task<IReadOnlyList<SitemapContentEntry>> GetPublishedSitemapEntriesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SitemapContentEntry>>(store.GetPublishedNewsItems()
            .Select(item => new SitemapContentEntry(item.Id, item.Title, item.PublishedAt, item.Slug))
            .ToList());

    public Task<NewsSearchPage> SearchAsync(string query, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Task.FromResult(new NewsSearchPage([], 0, page, pageSize));
        }

        var term = query.Trim();
        var matches = store.GetPublishedNewsItems()
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