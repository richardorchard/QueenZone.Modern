using QueenZone.Data;

namespace QueenZone.NewsAgent;

public sealed class RssAtomSourceFetcher(INewsDiscoveryHttpClient httpClient) : INewsSourceFetcher
{
    public NewsDiscoverySourceType SourceType => NewsDiscoverySourceType.Rss;

    public async Task<IReadOnlyList<FetchedNewsItem>> FetchAsync(
        NewsDiscoverySource source,
        CancellationToken cancellationToken = default)
    {
        var feedUrl = source.FeedOrSiteUrl
            ?? throw new InvalidOperationException($"Source '{source.Key}' does not have a feed URL configured.");

        var feedXml = await httpClient.GetStringAsync(feedUrl, cancellationToken);
        return NewsFeedParser.Parse(feedXml);
    }
}
