using QueenZone.Web.Sitemap;

namespace QueenZone.Web.Tests;

public sealed class SiteUrlTests
{
    [Theory]
    [InlineData("https://www.queenzone.org", "/news", "https://www.queenzone.org/news")]
    [InlineData("https://www.queenzone.org/", "/news/page/2", "https://www.queenzone.org/news/page/2")]
    [InlineData("https://preview.example.com", "articles", "https://preview.example.com/articles")]
    public void ToAbsolute_NormalizesBaseUrlAndPath(string baseUrl, string path, string expected)
    {
        Assert.Equal(expected, SiteUrl.ToAbsolute(baseUrl, path));
    }
}