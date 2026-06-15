namespace QueenZone.Data;

public interface INewsRepository
{
    Task<IReadOnlyList<NewsItem>> GetLatestAsync(int count, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NewsItem>> GetArchivePageAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    Task<int> GetPublishedCountAsync(CancellationToken cancellationToken = default);

    Task<NewsItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
