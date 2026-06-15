using QueenZone.Data;

namespace QueenZone.Web.Tests;

internal sealed class InMemoryNewsRepository : INewsRepository
{
    private readonly IReadOnlyList<NewsItem> publishedItems;

    public InMemoryNewsRepository(IEnumerable<NewsItem> rawItems)
    {
        publishedItems = rawItems
            .Where(item => item.IsPublished)
            .GroupBy(item => item.Id)
            .Select(group => group.OrderByDescending(item => item.PublishedAt).ThenByDescending(item => item.Id).First())
            .OrderByDescending(item => item.PublishedAt)
            .ThenByDescending(item => item.Id)
            .ToList();
    }

    public Task<IReadOnlyList<NewsItem>> GetLatestAsync(int count, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<NewsItem>>(publishedItems.Take(count).ToList());

    public Task<IReadOnlyList<NewsItem>> GetArchivePageAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var skip = Math.Max(page - 1, 0) * pageSize;
        return Task.FromResult<IReadOnlyList<NewsItem>>(publishedItems.Skip(skip).Take(pageSize).ToList());
    }

    public Task<int> GetPublishedCountAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(publishedItems.Count);

    public Task<NewsItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(publishedItems.SingleOrDefault(item => item.Id == id));
}