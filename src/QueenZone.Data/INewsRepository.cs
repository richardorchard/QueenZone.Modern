namespace QueenZone.Data;

/// <summary>
/// Public published-news reader used by archive pages and the sitemap.
/// </summary>
/// <remarks>
/// This is the extension point for future composite readers that union legacy
/// <c>NEWS_T</c> with modern approved-news tables (issue #7 / news-agent workflow).
/// Today's SQL-backed implementation is <see cref="EfNewsRepository"/>, which
/// projects published latest rows via <see cref="PublishedNewsQuery"/>.
/// </remarks>
public interface INewsRepository
{
    Task<IReadOnlyList<NewsItem>> GetLatestAsync(int count, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NewsItem>> GetArchivePageAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    Task<int> GetPublishedCountAsync(CancellationToken cancellationToken = default);

    Task<NewsItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SitemapContentEntry>> GetPublishedSitemapEntriesAsync(CancellationToken cancellationToken = default);
}
