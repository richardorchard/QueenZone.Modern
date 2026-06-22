using QueenZone.Data;

namespace QueenZone.Web.Tests;

internal sealed class InMemoryStoriesRepository : IStoriesRepository
{
    private readonly IReadOnlyList<StoryItem> publishedItems;

    public InMemoryStoriesRepository(IEnumerable<StoryItem> rawItems)
    {
        publishedItems = StoryItemOrdering.ByCreatedDateDescending(
            rawItems.Where(item => item.IsPublished));
    }

    public Task<IReadOnlyList<StoryItem>> GetLatestAsync(int count, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<StoryItem>>(publishedItems.Take(count).ToList());

    public Task<IReadOnlyList<StoryItem>> GetArchivePageAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var skip = Math.Max(page - 1, 0) * pageSize;
        return Task.FromResult<IReadOnlyList<StoryItem>>(publishedItems.Skip(skip).Take(pageSize).ToList());
    }

    public Task<int> GetPublishedCountAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(publishedItems.Count);

    public Task<StoryItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(publishedItems.SingleOrDefault(item => item.Id == id));
}