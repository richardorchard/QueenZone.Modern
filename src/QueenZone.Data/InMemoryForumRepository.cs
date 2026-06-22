namespace QueenZone.Data;

public sealed class InMemoryForumRepository(
    IReadOnlyList<ForumCategoryItem> seedCategories,
    ForumArchiveStats seedStats) : IForumRepository
{
    public Task<IReadOnlyList<ForumCategoryItem>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(seedCategories);

    public Task<ForumArchiveStats> GetArchiveStatsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(seedStats);
}