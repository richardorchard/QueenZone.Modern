using QueenZone.NewsAgent;

namespace QueenZone.Web.Tests;

public sealed class NewsDiscoveryUrlFilterTests
{
    [Theory]
    [InlineData("https://www.rogertaylorofficial.com/wp-content/themes/rogertaylor/_/css/reset.css?ver=4.7.33")]
    [InlineData("https://www.rogertaylorofficial.com/wp-content/uploads/2013/10/copy-Strange-Frontier-front.jpg")]
    [InlineData("https://www.rogertaylorofficial.com/wp-includes/wlwmanifest.xml")]
    [InlineData("https://www.rogertaylorofficial.com/xmlrpc.php?rsd")]
    [InlineData("https://www.rogertaylorofficial.com/wp-json/")]
    [InlineData("https://www.rogertaylorofficial.com/news/feed/")]
    [InlineData("https://www.rogertaylorofficial.com/news/")]
    public void IsLikelyArticleUrl_rejects_static_assets_and_wordpress_infrastructure(string url)
    {
        Assert.False(NewsDiscoveryUrlFilter.IsLikelyArticleUrl(url));
    }

    [Theory]
    [InlineData("https://www.rogertaylorofficial.com/roger-taylor-album-announce-reviews/")]
    [InlineData("https://www.rogertaylorofficial.com/news/tour-update")]
    [InlineData("https://www.queenonline.com/news/tour-2026")]
    [InlineData("https://www.billboard.com/music/pop/madonna-preview-confessions-album-livestream-1236285561/")]
    public void IsLikelyArticleUrl_accepts_story_urls(string url)
    {
        Assert.True(NewsDiscoveryUrlFilter.IsLikelyArticleUrl(url));
    }
}
