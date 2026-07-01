using QueenZone.Data;

namespace QueenZone.NewsAgent;

public interface INewsSourceFetcher
{
    NewsDiscoverySourceType SourceType { get; }

    Task<IReadOnlyList<FetchedNewsItem>> FetchAsync(
        NewsDiscoverySource source,
        CancellationToken cancellationToken = default);
}
