namespace QueenZone.Data;

public sealed class InMemoryNewsRepository(
    SharedNewsStore store,
    INewsSuggestionRepository? newsSuggestionRepository = null) : INewsRepository
{
    public async Task<IReadOnlyList<NewsItem>> GetLatestAsync(int count, CancellationToken cancellationToken = default)
    {
        var published = store.GetPublishedNewsItems();
        return await AddSubmissionAttributionAsync(published.Take(count).ToList(), cancellationToken);
    }

    public async Task<IReadOnlyList<NewsItem>> GetArchivePageAsync(
        int page,
        int pageSize,
        NewsArchiveFilter filter = default,
        CancellationToken cancellationToken = default)
    {
        var published = NewsArchiveFiltering.Apply(store.GetPublishedNewsItems(), filter);
        var skip = Math.Max(page - 1, 0) * pageSize;
        return await AddSubmissionAttributionAsync(published.Skip(skip).Take(pageSize).ToList(), cancellationToken);
    }

    public Task<int> GetPublishedCountAsync(NewsArchiveFilter filter = default, CancellationToken cancellationToken = default) =>
        Task.FromResult(NewsArchiveFiltering.Apply(store.GetPublishedNewsItems(), filter).Count);

    public Task<NewsArchiveYearRange> GetArchiveYearRangeAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(NewsArchiveYearRanges.Compute(store.GetPublishedNewsItems()));

    public async Task<NewsItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var item = store.GetPublishedNewsItems().SingleOrDefault(item => item.Id == id);
        return item is null
            ? null
            : (await AddSubmissionAttributionAsync([item], cancellationToken))[0];
    }

    public Task<IReadOnlyList<SitemapContentEntry>> GetPublishedSitemapEntriesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SitemapContentEntry>>(store.GetPublishedNewsItems()
            .Select(item => new SitemapContentEntry(item.Id, item.Title, item.PublishedAt, item.Slug))
            .ToList());

    public async Task<NewsSearchPage> SearchAsync(string query, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new NewsSearchPage([], 0, page, pageSize);
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

        var attributedItems = await AddSubmissionAttributionAsync(items, cancellationToken);
        return new NewsSearchPage(attributedItems, matches.Count, normalizedPage, pageSize);
    }

    private async Task<IReadOnlyList<NewsItem>> AddSubmissionAttributionAsync(
        IReadOnlyList<NewsItem> items,
        CancellationToken cancellationToken)
    {
        if (items.Count == 0 || newsSuggestionRepository is null)
        {
            return items;
        }

        var attributions = await newsSuggestionRepository.GetPromotedAttributionsAsync(
            items.Select(item => item.Id).Distinct().ToArray(),
            cancellationToken);
        var byNewsId = attributions.ToDictionary(attribution => attribution.NewsId);
        return items.Select(item => byNewsId.TryGetValue(item.Id, out var attribution)
                ? item with
                {
                    SubmitterMemberId = attribution.MemberId,
                    SubmitterDisplayName = attribution.DisplayName,
                }
                : item)
            .ToList();
    }
}
