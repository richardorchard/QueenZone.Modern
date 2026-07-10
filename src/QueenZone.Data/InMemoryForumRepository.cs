namespace QueenZone.Data;

public sealed class InMemoryForumRepository(
    IReadOnlyList<ForumCategoryItem> seedCategories,
    ForumArchiveStats seedStats,
    InMemoryForumWriteRepository? writeRepository = null) : IForumRepository
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
            .Concat(GetCreatedTopics(forumId))
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
        var header = SampleForumData.TryGetSeedTopicHeader(topicId)
            ?? TryGetCreatedTopicHeader(topicId);
        if (header is null)
        {
            return Task.FromResult<ForumTopicPostsPage?>(null);
        }

        var posts = SampleForumData.CreateSeedPosts(topicId)
            .Concat(GetCreatedPosts(topicId))
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

    public Task<ForumSearchPage> SearchForumAsync(
        string query,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Task.FromResult(new ForumSearchPage([], 0, page, pageSize));
        }

        var allResults = seedCategories
            .SelectMany(category => SampleForumData.CreateSeedTopics(category.Id)
                .Concat(GetCreatedTopics(category.Id))
                .Where(topic => topic.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Select(topic => new ForumSearchResult(
                    topic.Id,
                    topic.Title,
                    category.Id,
                    category.Name,
                    topic.ReplyCount,
                    topic.LastActivityAt,
                    topic.AuthorUsername)))
            .OrderByDescending(r => r.LastActivityAt)
            .ToList();

        var skip = Math.Max(page - 1, 0) * pageSize;
        var pageItems = allResults.Skip(skip).Take(pageSize).ToList();
        return Task.FromResult(new ForumSearchPage(pageItems, allResults.Count, page, pageSize));
    }

    private IReadOnlyList<ForumTopicItem> GetCreatedTopics(int forumId) =>
        writeRepository?.GetCreatedThreads()
            .Where(thread => thread.CategoryId == forumId)
            .Select(thread => new ForumTopicItem(
                thread.TopicId,
                thread.Subject,
                thread.LastPostAt.UtcDateTime,
                "Member",
                Math.Max(thread.PostCount - 1, 0),
                null,
                IsSticky: false))
            .ToList() ?? [];

    private ForumTopicHeader? TryGetCreatedTopicHeader(int topicId)
    {
        var thread = writeRepository?.GetCreatedThreads()
            .SingleOrDefault(thread => thread.TopicId == topicId);
        if (thread is null)
        {
            return null;
        }

        var category = seedCategories.SingleOrDefault(category => category.Id == thread.CategoryId);
        return category is null
            ? null
            : new ForumTopicHeader(thread.TopicId, thread.Subject, category.Id, category.Name);
    }

    private IReadOnlyList<ForumPostItem> GetCreatedPosts(int topicId) =>
        writeRepository?.GetPostsForTopic(topicId)
            .Select(post => new ForumPostItem(
                post.PostId,
                post.Body,
                post.CreatedAt.UtcDateTime,
                post.DisplayName,
                Signature: null,
                AuthorPostCount: 0,
                AuthorMemberSince: null))
            .ToList() ?? [];
}
