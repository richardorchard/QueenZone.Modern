using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class HomeImageDeliveryTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public HomeImageDeliveryTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
    }

    [Fact]
    public async Task HomePageServesWebpVariantsForBelowTheFoldImages()
    {
        var client = factory.CreateClient();
        var body = await client.GetStringAsync("/");

        Assert.Contains("type=\"image/webp\"", body);
        Assert.Contains("/design-system/assets/img-portrait.webp", body);
        Assert.Contains("/design-system/assets/img-crowd.webp", body);
        Assert.Contains("/design-system/assets/img-stage.webp", body);
        Assert.Contains("/design-system/assets/img-studio.webp", body);
        Assert.Contains("loading=\"lazy\"", body);
        Assert.Contains("fetchpriority=\"high\"", body);
    }

    [Fact]
    public async Task HomePageKeepsJpgFallbackForBelowTheFoldImages()
    {
        var client = factory.CreateClient();
        var body = await client.GetStringAsync("/");

        Assert.Contains("/design-system/assets/img-portrait.jpg", body);
        Assert.Contains("/design-system/assets/img-crowd.jpg", body);
        Assert.Contains("/design-system/assets/img-stage.jpg", body);
        Assert.Contains("/design-system/assets/img-studio.jpg", body);
    }
}
