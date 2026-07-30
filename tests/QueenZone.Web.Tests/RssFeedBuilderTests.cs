using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class RssFeedBuilderTests
{
    [Fact]
    public void Build_EmitsRssChannelAndItems()
    {
        var xml = RssFeedBuilder.Build(
            "QueenZone News",
            "https://preview.queenzone.test/news",
            "Latest news",
            "https://preview.queenzone.test/news/feed.rss",
            [
                new RssFeedBuilder.Item(
                    "Hello <Queen>",
                    "https://preview.queenzone.test/news/1/hello-queen",
                    "Excerpt & more",
                    new DateTime(2026, 6, 11, 9, 0, 0, DateTimeKind.Utc)),
            ]);

        Assert.Contains("<rss version=\"2.0\"", xml);
        Assert.Contains("<title>QueenZone News</title>", xml);
        Assert.Contains("atom:link href=\"https://preview.queenzone.test/news/feed.rss\"", xml);
        Assert.Contains("<title>Hello &lt;Queen&gt;</title>", xml);
        Assert.Contains("<description>Excerpt &amp; more</description>", xml);
        Assert.Contains("<guid isPermaLink=\"true\">https://preview.queenzone.test/news/1/hello-queen</guid>", xml);
        Assert.Contains("<lastBuildDate>", xml);
    }

    [Fact]
    public void EscapeXml_EncodesMarkupCharacters()
    {
        Assert.Equal("&lt;a&gt;&amp;&quot;x&apos;", RssFeedBuilder.EscapeXml("<a>&\"x'"));
    }
}
