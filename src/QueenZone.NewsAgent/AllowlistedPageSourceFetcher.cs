using QueenZone.Data;

namespace QueenZone.NewsAgent;

public sealed class AllowlistedPageSourceFetcher(INewsDiscoveryHttpClient httpClient) : INewsSourceFetcher
{
    public NewsDiscoverySourceType SourceType => NewsDiscoverySourceType.AllowlistedPage;

    public async Task<IReadOnlyList<FetchedNewsItem>> FetchAsync(
        NewsDiscoverySource source,
        CancellationToken cancellationToken = default)
    {
        var pageUrl = source.FeedOrSiteUrl ?? source.HomepageUrl;
        if (!Uri.TryCreate(pageUrl, UriKind.Absolute, out var pageUri))
        {
            throw new InvalidOperationException($"Source '{source.Key}' does not have a valid allowlisted page URL.");
        }

        var html = await httpClient.GetStringAsync(pageUri.AbsoluteUri, cancellationToken);
        return NewsFeedParser.ParseAllowlistedPageLinks(html, pageUri);
    }
}
