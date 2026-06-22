namespace QueenZone.Data;

public interface IForumRepository
{
    Task<IReadOnlyList<ForumCategoryItem>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    Task<ForumArchiveStats> GetArchiveStatsAsync(CancellationToken cancellationToken = default);
}