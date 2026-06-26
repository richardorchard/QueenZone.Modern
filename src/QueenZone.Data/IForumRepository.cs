namespace QueenZone.Data;

public interface IForumRepository
{
    Task<IReadOnlyList<ForumCategoryItem>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    Task<ForumCategoryItem?> GetCategoryByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<ForumCategoryTopicsPage> GetCategoryTopicsPageAsync(
        int forumId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ForumTopicPostsPage?> GetTopicPostsPageAsync(
        int topicId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> GetTotalThreadCountAsync(CancellationToken cancellationToken = default);

    Task<ForumArchiveStats> GetArchiveStatsAsync(CancellationToken cancellationToken = default);

    Task<int> GetTopicSitemapCountAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ForumTopicSitemapItem>> GetTopicSitemapPageAsync(
        int offset,
        int pageSize,
        CancellationToken cancellationToken = default);
}