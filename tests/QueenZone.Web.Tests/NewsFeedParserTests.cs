using QueenZone.Data;
using QueenZone.NewsAgent;

namespace QueenZone.Web.Tests;

public sealed class NewsFeedParserTests
{
    [Fact]
    public void Parse_reads_rss_items_with_title_link_and_excerpt()
    {
        var feed = NewsAgentTestSupport.ReadFixture("sample-rss.xml");

        var items = NewsFeedParser.Parse(feed);

        Assert.Equal(2, items.Count);
        Assert.Equal("Queen announce 2026 tour", items[0].Title);
        Assert.Equal("https://www.queenonline.com/news/tour-2026?utm_source=test", items[0].SourceUrl);
        Assert.Equal("Official tour dates announced.", items[0].Excerpt);
        Assert.NotNull(items[0].PublishedAt);
    }

    [Fact]
    public void ParseSitemap_reads_locations_and_last_modified_dates()
    {
        var sitemap = NewsAgentTestSupport.ReadFixture("sample-sitemap.xml");

        var items = NewsFeedParser.ParseSitemap(sitemap);

        Assert.Equal(2, items.Count);
        Assert.Equal("https://www.queenonline.com/news/sitemap-story", items[0].SourceUrl);
        Assert.NotNull(items[0].PublishedAt);
    }

    [Fact]
    public void ParseAllowlistedPageLinks_keeps_same_host_links_only()
    {
        var html = NewsAgentTestSupport.ReadFixture("sample-page.html");
        var pageUri = new Uri("https://www.rogertaylorofficial.com/news");

        var items = NewsFeedParser.ParseAllowlistedPageLinks(html, pageUri);

        Assert.Equal(2, items.Count);
        Assert.All(items, item => Assert.Contains("rogertaylorofficial.com", item.SourceUrl));
    }
}
