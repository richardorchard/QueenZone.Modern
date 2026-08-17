using QueenZone.Web.Sitemap;

namespace QueenZone.Web.Tests;

public sealed class SocialShareRoutesTests : IClassFixture<QueenZoneWebApplicationFactory>
{
    private readonly QueenZoneWebApplicationFactory factory;

    public SocialShareRoutesTests(QueenZoneWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Theory]
    [InlineData(
        "/news/1003/queenzone-modernisation-begins",
        "/news/1003/queenzone-modernisation-begins",
        "QueenZone modernisation begins | QueenZone news")]
    [InlineData(
        "/articles/101/inside-the-making-of-bohemian-rhapsody",
        "/articles/101/inside-the-making-of-bohemian-rhapsody",
        "Inside the Making of Bohemian Rhapsody | QueenZone articles")]
    [InlineData(
        "/biography/2/1970",
        "/biography/2/1970",
        "1970 | QueenZone biography")]
    [InlineData(
        "/photography/brian-may/101",
        "/photography/brian-may/101",
        "Brian in action with his guitar | Brian May | Photography | QueenZone")]
    public async Task ContentDetailPages_RenderStaticShareFallbackWithNativeHook(
        string path,
        string canonicalPath,
        string title)
    {
        var body = await factory.CreateClient().GetStringAsync(path);

        AssertStaticShareMarkup(body, canonicalPath, title);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/news")]
    [InlineData("/articles")]
    [InlineData("/biography")]
    [InlineData("/photography")]
    public async Task NonDetailPages_DoNotRenderShareControls(string path)
    {
        var body = await factory.CreateClient().GetStringAsync(path);

        Assert.DoesNotContain("data-share-url=", body);
        Assert.DoesNotContain("twitter.com/intent/tweet", body);
        Assert.DoesNotContain("facebook.com/sharer/sharer.php", body);
    }

    private static void AssertStaticShareMarkup(string body, string canonicalPath, string title)
    {
        var shareUrl = SiteUrl.ToAbsolute(TestSiteConfiguration.PublicBaseUrl, canonicalPath);
        var encodedUrl = Uri.EscapeDataString(shareUrl);
        var encodedText = Uri.EscapeDataString(title);

        Assert.Contains($"data-share-url=\"{shareUrl}\"", body);
        Assert.Contains($"data-share-title=\"{title}\"", body);
        Assert.Contains("Share on X", body);
        Assert.Contains("Share on Facebook", body);
        Assert.Contains("Share on WhatsApp", body);
        Assert.Contains("Share by email", body);
        Assert.Contains("https://twitter.com/intent/tweet", body);
        Assert.Contains($"text={encodedText}", body);
        Assert.Contains($"url={encodedUrl}", body);
        Assert.Contains("https://www.facebook.com/sharer/sharer.php", body);
        Assert.Contains($"u={encodedUrl}", body);
        Assert.Contains("https://api.whatsapp.com/send", body);
        Assert.Contains(Uri.EscapeDataString($"{title} {shareUrl}"), body);
        Assert.Contains($"mailto:?subject={encodedText}", body);
        Assert.Contains($"body={encodedUrl}", body);
        Assert.Contains("data-share-native hidden", body);
        Assert.Contains("data-share-fallback", body);
    }
}
