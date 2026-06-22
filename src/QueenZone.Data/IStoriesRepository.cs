namespace QueenZone.Data;

public interface IStoriesRepository
{
    Task<IReadOnlyList<StoryItem>> GetLatestAsync(int count, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StoryItem>> GetArchivePageAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    Task<int> GetPublishedCountAsync(CancellationToken cancellationToken = default);

    Task<StoryItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}