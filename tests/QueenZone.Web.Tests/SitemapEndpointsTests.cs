using System.Xml.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace QueenZone.Web.Tests;

public sealed class SitemapEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string BaseUrl = "https://www.queenzone.org";
    private readonly WebApplicationFactory<Program> factory;

    public SitemapEndpointsTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
    }

    [Fact]
    public async Task RobotsTxt_ReferencesProductionSitemapAndBlocksAdmin()
    {
        var client = factory.CreateClient();

        var body = await client.GetStringAsync("/robots.txt");

        Assert.Contains($"Sitemap: {BaseUrl}/sitemap.xml", body);
        Assert.Contains("Disallow: /admin/", body);
        Assert.Contains("Disallow: /health", body);
    }

    [Fact]
    public async Task SitemapIndex_ReferencesCoreSitemap()
    {
        var client = factory.CreateClient();

        var xml = await client.GetStringAsync("/sitemap.xml");
        var document = XDocument.Parse(xml);
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

        var locations = document
            .Descendants(ns + "loc")
            .Select(element => element.Value)
            .ToList();

        Assert.Single(locations);
        Assert.Equal($"{BaseUrl}/sitemap-core.xml", locations[0]);
    }

    [Fact]
    public async Task CoreSitemap_IncludesPublicCanonicalUrlsOnly()
    {
        var client = factory.CreateClient();

        var xml = await client.GetStringAsync("/sitemap-core.xml");
        var document = XDocument.Parse(xml);
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

        var locations = document
            .Descendants(ns + "loc")
            .Select(element => element.Value)
            .ToList();

        Assert.Contains($"{BaseUrl}/", locations);
        Assert.Contains($"{BaseUrl}/news", locations);
        Assert.Contains($"{BaseUrl}/news/page/2", locations);
        Assert.Contains($"{BaseUrl}/news/1003/queenzone-modernisation-begins", locations);
        Assert.DoesNotContain($"{BaseUrl}/news/9001/hidden-moderation-draft", locations);
        Assert.Contains($"{BaseUrl}/articles", locations);
        Assert.Contains($"{BaseUrl}/articles/101/inside-the-making-of-bohemian-rhapsody", locations);
        Assert.DoesNotContain($"{BaseUrl}/articles/9001/hidden-moderation-draft", locations);
        Assert.Contains($"{BaseUrl}/biography", locations);
        Assert.Contains($"{BaseUrl}/biography/1/1946-1969", locations);
        Assert.Contains($"{BaseUrl}/forum", locations);
        Assert.Contains($"{BaseUrl}/forum/1/the-music", locations);
        Assert.Contains($"{BaseUrl}/photography", locations);
        Assert.Contains($"{BaseUrl}/photography/brian-may", locations);
        Assert.Contains($"{BaseUrl}/photography/queen", locations);
        Assert.Contains($"{BaseUrl}/photography/queen/201", locations);
        Assert.DoesNotContain($"{BaseUrl}/photography/empty-category", locations);
    }

    [Fact]
    public async Task CoreSitemap_UsesPublishedDateForNewsDetailLastMod()
    {
        var client = factory.CreateClient();

        var xml = await client.GetStringAsync("/sitemap-core.xml");
        var document = XDocument.Parse(xml);
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

        var newsDetail = document
            .Descendants(ns + "url")
            .Single(url => url.Element(ns + "loc")?.Value == $"{BaseUrl}/news/1003/queenzone-modernisation-begins");

        Assert.Equal("2026-06-11", newsDetail.Element(ns + "lastmod")?.Value);
    }

    [Fact]
    public async Task CoreSitemap_SetsPublicCacheControlHeader()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/sitemap-core.xml");

        response.EnsureSuccessStatusCode();
        Assert.Equal("public, max-age=86400", response.Headers.CacheControl?.ToString());
    }
}