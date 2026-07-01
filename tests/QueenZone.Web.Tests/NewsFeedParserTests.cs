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

    [Fact]
    public void ParseAllowlistedPageLinks_skips_static_asset_links()
    {
        const string html = """
            <!DOCTYPE html>
            <html>
              <body>
                <a href="/news/tour-update">Tour update</a>
                <a href="/wp-content/themes/site/style.css">Stylesheet</a>
                <a href="/wp-content/uploads/hero.jpg">Hero image</a>
                <a href="/news/feed/">Feed</a>
              </body>
            </html>
            """;
        var pageUri = new Uri("https://www.rogertaylorofficial.com/news");

        var items = NewsFeedParser.ParseAllowlistedPageLinks(html, pageUri);

        Assert.Single(items);
        Assert.Equal("https://www.rogertaylorofficial.com/news/tour-update", items[0].SourceUrl);
    }

    [Fact]
    public void ParseAllowlistedPageLinks_limits_links_to_page_path_prefix()
    {
        const string html = """
            <!DOCTYPE html>
            <html>
              <body>
                <a href="/news/tour-update">Tour update</a>
                <a href="/music">Music section</a>
                <a href="/news/feed/">Feed</a>
              </body>
            </html>
            """;
        var pageUri = new Uri("https://www.queenonline.com/news");

        var items = NewsFeedParser.ParseAllowlistedPageLinks(html, pageUri);

        Assert.Single(items);
        Assert.Equal("https://www.queenonline.com/news/tour-update", items[0].SourceUrl);
    }

    [Fact]
    public void Parse_reads_atom_entries()
    {
        const string atom = """
            <?xml version="1.0" encoding="utf-8"?>
            <feed xmlns="http://www.w3.org/2005/Atom">
              <entry>
                <title>Queen box set announced</title>
                <link href="https://www.queenonline.com/news/box-set" rel="alternate" />
                <summary>Collector edition announced.</summary>
                <published>2026-07-01T10:00:00Z</published>
              </entry>
            </feed>
            """;

        var items = NewsFeedParser.Parse(atom);

        Assert.Single(items);
        Assert.Equal("Queen box set announced", items[0].Title);
        Assert.Equal("https://www.queenonline.com/news/box-set", items[0].SourceUrl);
    }
}
