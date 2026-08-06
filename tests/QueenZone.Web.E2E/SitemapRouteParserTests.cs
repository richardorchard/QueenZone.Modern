namespace QueenZone.Web.E2E;

[TestFixture]
[Category(E2ECategories.Deterministic)]
[Category(E2ECategories.ReadOnly)]
public class SitemapRouteParserTests
{
    [Test]
    public void ParseIndexPaths_ReturnsPathsFromAbsoluteLocations()
    {
        const string xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <sitemapindex xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
              <sitemap><loc>https://www.queenzone.org/sitemap-core.xml</loc></sitemap>
              <sitemap><loc>https://www.queenzone.org/sitemap-forum-2.xml?part=latest</loc></sitemap>
            </sitemapindex>
            """;

        var paths = SitemapRouteParser.ParseIndexPaths(xml);

        Assert.That(paths, Is.EqualTo(new[]
        {
            "/sitemap-core.xml",
            "/sitemap-forum-2.xml?part=latest",
        }));
    }

    [Test]
    public void ParseUrlSetPaths_SupportsNamespaceLessXmlAndRelativeLocations()
    {
        const string xml = """
            <urlset>
              <url><loc>/news</loc></url>
              <url><loc> /news/42/a-story </loc></url>
            </urlset>
            """;

        var paths = SitemapRouteParser.ParseUrlSetPaths(xml);

        Assert.That(paths, Is.EqualTo(new[] { "/news", "/news/42/a-story" }));
    }

    [Test]
    public void ParseUrlSetPaths_SupportsPrefixedSitemapNamespace()
    {
        const string xml = """
            <sm:urlset xmlns:sm="http://www.sitemaps.org/schemas/sitemap/0.9">
              <sm:url><sm:loc>https://www.queenzone.org/articles/a-story</sm:loc></sm:url>
            </sm:urlset>
            """;

        var paths = SitemapRouteParser.ParseUrlSetPaths(xml);

        Assert.That(paths, Is.EqualTo(new[] { "/articles/a-story" }));
    }

    [Test]
    public void ParseIndexPaths_RejectsUrlSetInsteadOfFollowingPageUrlsAsSitemaps()
    {
        const string xml = "<urlset><url><loc>/news</loc></url></urlset>";

        var exception = Assert.Throws<FormatException>(() => SitemapRouteParser.ParseIndexPaths(xml));

        Assert.That(exception!.Message, Does.Contain("Expected sitemap root 'sitemapindex'"));
    }

    [TestCase("/sitemap-core.xml", "core")]
    [TestCase("/sitemap-news.xml", "news")]
    [TestCase("/sitemap-forum-1.xml", "forum")]
    [TestCase("/sitemap-forum-12.xml", "forum")]
    [TestCase("/sitemap-forum-12.xml?part=latest", "forum")]
    [TestCase("/custom.xml", "custom.xml")]
    public void ResolveSectionName_GroupsSitemapFiles(string path, string expected)
    {
        Assert.That(SitemapRouteParser.ResolveSectionName(path), Is.EqualTo(expected));
    }
}
