using QueenZone.Data;
using QueenZone.NewsAgent;

namespace QueenZone.Web.Tests;

public sealed class NewsAgentFetcherTests
{
    [Fact]
    public async Task RssAtomSourceFetcher_reads_configured_feed_url()
    {
        var feedUrl = "https://www.queenonline.com/feed/";
        var fetcher = new RssAtomSourceFetcher(new FakeNewsDiscoveryHttpClient(new Dictionary<string, string>
        {
            [feedUrl] = NewsAgentTestSupport.ReadFixture("sample-rss.xml")
        }));
        var source = CreateSource(NewsDiscoverySourceType.Rss, feedUrl);

        var items = await fetcher.FetchAsync(source);

        Assert.Equal(2, items.Count);
    }

    [Fact]
    public async Task SitemapSourceFetcher_reads_configured_sitemap_url()
    {
        var sitemapUrl = "https://www.queenonline.com/sitemap.xml";
        var fetcher = new SitemapSourceFetcher(new FakeNewsDiscoveryHttpClient(new Dictionary<string, string>
        {
            [sitemapUrl] = NewsAgentTestSupport.ReadFixture("sample-sitemap.xml")
        }));
        var source = CreateSource(NewsDiscoverySourceType.Sitemap, sitemapUrl);

        var items = await fetcher.FetchAsync(source);

        Assert.Equal(2, items.Count);
    }

    [Fact]
    public async Task AllowlistedPageSourceFetcher_reads_configured_page()
    {
        var pageUrl = "https://www.rogertaylorofficial.com/news";
        var fetcher = new AllowlistedPageSourceFetcher(new FakeNewsDiscoveryHttpClient(new Dictionary<string, string>
        {
            [pageUrl] = NewsAgentTestSupport.ReadFixture("sample-page.html")
        }));
        var source = CreateSource(NewsDiscoverySourceType.AllowlistedPage, pageUrl);

        var items = await fetcher.FetchAsync(source);

        Assert.Equal(2, items.Count);
    }

    [Fact]
    public async Task RssAtomSourceFetcher_throws_when_feed_url_missing()
    {
        var fetcher = new RssAtomSourceFetcher(new FakeNewsDiscoveryHttpClient(new Dictionary<string, string>()));
        var source = CreateSource(NewsDiscoverySourceType.Rss, null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => fetcher.FetchAsync(source));
    }

    [Fact]
    public void FetcherRegistry_throws_for_unregistered_source_type()
    {
        var registry = new NewsSourceFetcherRegistry([]);

        Assert.Throws<InvalidOperationException>(() => registry.GetFetcher(NewsDiscoverySourceType.Rss));
    }

    private static NewsDiscoverySource CreateSource(NewsDiscoverySourceType sourceType, string? feedOrSiteUrl) =>
        new(
            1,
            "test-source",
            "Test Source",
            "https://example.com/",
            feedOrSiteUrl,
            sourceType,
            NewsDiscoveryTrustTier.Primary,
            60,
            true,
            null,
            null,
            DateTime.UtcNow,
            DateTime.UtcNow);
}
