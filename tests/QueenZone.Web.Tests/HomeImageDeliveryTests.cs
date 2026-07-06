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
        Assert.Contains("img-portrait.webp?v=", body);
        Assert.Contains("img-crowd.webp?v=", body);
        Assert.Contains("img-stage.webp?v=", body);
        Assert.Contains("img-studio.webp?v=", body);
        Assert.Contains("loading=\"lazy\"", body);
        Assert.Contains("fetchpriority=\"high\"", body);
    }

    [Fact]
    public async Task HomePageKeepsJpgFallbackForBelowTheFoldImages()
    {
        var client = factory.CreateClient();
        var body = await client.GetStringAsync("/");

        Assert.Contains("img-portrait.jpg?v=", body);
        Assert.Contains("img-crowd.jpg?v=", body);
        Assert.Contains("img-stage.jpg?v=", body);
        Assert.Contains("img-studio.jpg?v=", body);
    }
}
