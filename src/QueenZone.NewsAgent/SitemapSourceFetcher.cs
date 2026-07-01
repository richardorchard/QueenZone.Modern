using QueenZone.Data;

namespace QueenZone.NewsAgent;

public sealed class SitemapSourceFetcher(INewsDiscoveryHttpClient httpClient) : INewsSourceFetcher
{
    public NewsDiscoverySourceType SourceType => NewsDiscoverySourceType.Sitemap;

    public async Task<IReadOnlyList<FetchedNewsItem>> FetchAsync(
        NewsDiscoverySource source,
        CancellationToken cancellationToken = default)
    {
        var sitemapUrl = source.FeedOrSiteUrl
            ?? throw new InvalidOperationException($"Source '{source.Key}' does not have a sitemap URL configured.");

        var sitemapXml = await httpClient.GetStringAsync(sitemapUrl, cancellationToken);
        return NewsFeedParser.ParseSitemap(sitemapXml);
    }
}
