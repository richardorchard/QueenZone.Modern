using QueenZone.Data;

namespace QueenZone.NewsAgent;

public sealed class NewsSourceFetcherRegistry(IEnumerable<INewsSourceFetcher> fetchers)
{
    public INewsSourceFetcher GetFetcher(NewsDiscoverySourceType sourceType) =>
        fetchers.FirstOrDefault(fetcher => fetcher.SourceType == sourceType)
        ?? throw new InvalidOperationException($"No fetcher is registered for source type '{sourceType}'.");
}
