using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class VersionedStaticImageTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public VersionedStaticImageTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
    }

    [Fact]
    public async Task HomePageVersionsDesignSystemImagesForLongLivedCaching()
    {
        var body = await factory.CreateClient().GetStringAsync("/");

        Assert.Contains("img-portrait.webp?v=", body);
        Assert.Contains("img-portrait.jpg?v=", body);
        Assert.Contains("img-crowd.webp?v=", body);
        Assert.Contains("img-stage.jpg?v=", body);
        Assert.Contains("img-studio.webp?v=", body);
    }

    [Fact]
    public async Task HomePageEmbedsVersionedEraMontageConfig()
    {
        var body = await factory.CreateClient().GetStringAsync("/");

        Assert.Contains("id=\"qz-era-config\"", body);
        Assert.Contains("/assets/eras/queenzone-1999.png?v=", body);
        Assert.Contains("/assets/eras/queenzone-2020.png?v=", body);
    }

    [Fact]
    public async Task ArticleDetailPageVersionsHeroImage()
    {
        var body = await factory.CreateClient().GetStringAsync("/articles/101/inside-the-making-of-bohemian-rhapsody");

        Assert.Contains("/design-system/assets/img-hero.jpg?v=", body);
    }
}

public sealed class HomeEraMontageTests
{
    [Fact]
    public void GetSlides_versionsEachEraScreenshotPath()
    {
        var provider = new TestFileVersionProvider();

        var slides = HomeEraMontage.GetSlides(provider, PathString.Empty);

        Assert.Equal(5, slides.Count);
        Assert.All(slides, slide => Assert.Contains("?v=", slide.Img, StringComparison.Ordinal));
        Assert.Equal("/assets/eras/queenzone-1999.png?v=test-hash", slides[0].Img);
    }

    private sealed class TestFileVersionProvider : IFileVersionProvider
    {
        public string AddFileVersionToPath(PathString requestPathBase, string path) => $"{path}?v=test-hash";
    }
}
