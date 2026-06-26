namespace QueenZone.Data;

public sealed class InMemoryForumRepository(
    IReadOnlyList<ForumCategoryItem> seedCategories,
    ForumArchiveStats seedStats) : IForumRepository
{
    public Task<IReadOnlyList<ForumCategoryItem>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(seedCategories);

    public Task<ForumCategoryItem?> GetCategoryByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(seedCategories.SingleOrDefault(category => category.Id == id));

    public Task<ForumCategoryTopicsPage> GetCategoryTopicsPageAsync(
        int forumId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var topics = SampleForumData.CreateSeedTopics(forumId)
            .OrderByDescending(topic => topic.IsSticky)
            .ThenByDescending(topic => topic.LastActivityAt)
            .ToList();

        var skip = Math.Max(page - 1, 0) * pageSize;
        var pageItems = topics.Skip(skip).Take(pageSize).ToList();
        return Task.FromResult(new ForumCategoryTopicsPage(pageItems, topics.Count, page, pageSize));
    }

    public Task<ForumTopicPostsPage?> GetTopicPostsPageAsync(
        int topicId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var header = SampleForumData.TryGetSeedTopicHeader(topicId);
        if (header is null)
        {
            return Task.FromResult<ForumTopicPostsPage?>(null);
        }

        var posts = SampleForumData.CreateSeedPosts(topicId)
            .OrderBy(post => post.PostedAt)
            .ToList();
        var skip = Math.Max(page - 1, 0) * pageSize;
        var pageItems = posts.Skip(skip).Take(pageSize).ToList();
        return Task.FromResult<ForumTopicPostsPage?>(
            new ForumTopicPostsPage(header, pageItems, posts.Count, page, pageSize));
    }

    public Task<int> GetTotalThreadCountAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(seedStats.ThreadCount);

    public Task<ForumArchiveStats> GetArchiveStatsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(seedStats);

    public Task<int> GetTopicSitemapCountAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(SampleForumData.CreateSeedTopicSitemapItems().Count);

    public Task<IReadOnlyList<ForumTopicSitemapItem>> GetTopicSitemapPageAsync(
        int offset,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var page = SampleForumData.CreateSeedTopicSitemapItems()
            .Skip(Math.Max(offset, 0))
            .Take(pageSize)
            .ToList();
        return Task.FromResult<IReadOnlyList<ForumTopicSitemapItem>>(page);
    }
}