using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace QueenZone.Web.Tests;

public sealed class FreddieTributePageTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public FreddieTributePageTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
        });
    }

    [Fact]
    public async Task FreddieTributePage_RendersSeedTributesPhotosAndMenuLink()
    {
        var client = factory.CreateClient();

        var body = await client.GetStringAsync("/freddie-mercury-tribute");

        Assert.Contains("Freddie Mercury Tribute", body);
        Assert.Contains("Freddie Tribute", body);
        Assert.Contains("href=\"/freddie-mercury-tribute\"", body);
        Assert.Contains("Thank you for Bohemian Rhapsody", body);
        Assert.Contains("Featured tribute", body);
        Assert.Contains("Selected Freddie Mercury photographs", body);
        Assert.Contains("https://cdn.queenzone.org/freddie-mercury/", body);
        Assert.Contains("Page 1 of 2", body);
        Assert.Contains("href=\"/freddie-mercury-tribute/page/2\"", body);
    }

    [Fact]
    public async Task FreddieTributePageTwo_RendersRemainingSeedTribute()
    {
        var client = factory.CreateClient();

        var body = await client.GetStringAsync("/freddie-mercury-tribute/page/2");

        Assert.Contains("Page 2 of 2", body);
        Assert.Contains("A beautiful voice, a brilliant writer", body);
        Assert.Contains("rel=\"prev\" href=\"/freddie-mercury-tribute\"", body);
    }

    [Fact]
    public async Task FreddieTributePageBeyondArchive_ReturnsNotFound()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/freddie-mercury-tribute/page/3");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
