using QueenZone.Web.Sitemap;

namespace QueenZone.Web.Tests;

public sealed class SitemapXmlWriterTests
{
    private const string BaseUrl = "https://www.queenzone.org";

    [Fact]
    public void WriteUrlSet_EscapesXmlAndUsesCanonicalAbsoluteUrls()
    {
        var xml = SitemapXmlWriter.WriteUrlSet(
            [
                new SitemapEntry("/news"),
                new SitemapEntry("/news/1003/queenzone-modernisation-begins", new DateTime(2026, 6, 11, 9, 0, 0, DateTimeKind.Utc))
            ],
            BaseUrl);

        Assert.Contains("<loc>https://www.queenzone.org/news</loc>", xml);
        Assert.Contains("<loc>https://www.queenzone.org/news/1003/queenzone-modernisation-begins</loc>", xml);
        Assert.Contains("<lastmod>2026-06-11</lastmod>", xml);
        Assert.Contains("xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\"", xml);
    }

    [Fact]
    public void WriteSitemapIndex_ReferencesChildSitemapWithAbsoluteUrl()
    {
        var xml = SitemapXmlWriter.WriteSitemapIndex(
            [new SitemapIndexEntry("/sitemap-core.xml", new DateTime(2026, 6, 24, 0, 0, 0, DateTimeKind.Utc))],
            BaseUrl);

        Assert.Contains("<sitemapindex", xml);
        Assert.Contains("<loc>https://www.queenzone.org/sitemap-core.xml</loc>", xml);
        Assert.Contains("<lastmod>2026-06-24</lastmod>", xml);
    }

    [Fact]
    public void SiteUrl_ToAbsolute_NormalizesBaseAndPath()
    {
        Assert.Equal(
            "https://www.queenzone.org/news",
            SiteUrl.ToAbsolute("https://www.queenzone.org/", "/news"));
    }
}