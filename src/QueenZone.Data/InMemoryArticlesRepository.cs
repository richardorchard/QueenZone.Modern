namespace QueenZone.Data;

public sealed class InMemoryArticlesRepository(IReadOnlyList<ArticleItem> seedArticles) : IArticlesRepository
{
    private readonly IReadOnlyList<ArticleItem> publishedItems = ArticleItemOrdering.ByCreatedDateDescending(
        seedArticles.Where(item => item.IsPublished));

    public Task<IReadOnlyList<ArticleItem>> GetLatestAsync(int count, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ArticleItem>>(publishedItems.Take(count).ToList());

    public Task<IReadOnlyList<ArticleItem>> GetArchivePageAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var skip = Math.Max(page - 1, 0) * pageSize;
        return Task.FromResult<IReadOnlyList<ArticleItem>>(publishedItems.Skip(skip).Take(pageSize).ToList());
    }

    public Task<int> GetPublishedCountAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(publishedItems.Count);

    public Task<ArticleItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(publishedItems.SingleOrDefault(item => item.Id == id));

    public Task<IReadOnlyList<SitemapContentEntry>> GetPublishedSitemapEntriesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SitemapContentEntry>>(publishedItems
            .Select(item => new SitemapContentEntry(item.Id, item.Title, item.PublishedAt))
            .ToList());
}