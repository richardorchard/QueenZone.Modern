using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class SiteScriptDeliveryTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public SiteScriptDeliveryTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
    }

    [Fact]
    public async Task SharedLayout_DefersAndVersionsSiteJs()
    {
        var body = await factory.CreateClient().GetStringAsync("/news");

        Assert.Contains("/js/site.js?v=", body);
        Assert.Matches(@"<script[^>]+src=""[^""]*/js/site\.js\?v=[^""]+""[^>]*\bdefer\b", body);
        Assert.DoesNotContain("/js/home-archive-hero.js", body);
        Assert.DoesNotContain("qz-era-config", body);
    }

    [Fact]
    public async Task HomePage_LoadsHomepageHeroScriptAndKeepsSharedSiteJs()
    {
        var body = await factory.CreateClient().GetStringAsync("/");

        Assert.Contains("/js/site.js?v=", body);
        Assert.Matches(@"<script[^>]+src=""[^""]*/js/site\.js\?v=[^""]+""[^>]*\bdefer\b", body);
        Assert.Contains("/js/home-archive-hero.js?v=", body);
        Assert.Matches(@"<script[^>]+src=""[^""]*/js/home-archive-hero\.js\?v=[^""]+""[^>]*\bdefer\b", body);
        Assert.Contains("id=\"qz-era-config\"", body);
    }

    [Theory]
    [InlineData("/news")]
    [InlineData("/forum")]
    [InlineData("/articles")]
    [InlineData("/about")]
    public async Task NonHomePublicPages_DoNotLoadHomepageHeroScript(string path)
    {
        var body = await factory.CreateClient().GetStringAsync(path);

        Assert.Contains("/js/site.js?v=", body);
        Assert.DoesNotContain("/js/home-archive-hero.js", body);
        Assert.DoesNotContain("id=\"qz-era-config\"", body);
    }

    [Fact]
    public async Task HomeArchiveHeroScript_IsServed()
    {
        var response = await factory.CreateClient().GetAsync("/js/home-archive-hero.js");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var script = await response.Content.ReadAsStringAsync();
        Assert.Contains("qz-hero-archive", script);
        Assert.Contains("prefers-reduced-motion", script);
        Assert.Contains("visibilitychange", script);
        Assert.DoesNotContain("data-masthead", script);
    }

    [Fact]
    public async Task SiteJs_DoesNotContainHomepageEraMontage()
    {
        var script = await factory.CreateClient().GetStringAsync("/js/site.js");

        Assert.Contains("data-masthead", script);
        Assert.DoesNotContain("qz-era-config", script);
        Assert.DoesNotContain("qz-hero-archive", script);
    }
}
