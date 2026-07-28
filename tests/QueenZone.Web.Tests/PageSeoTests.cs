using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace QueenZone.Web.Tests;

public sealed class PageSeoTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public PageSeoTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
    }

    [Theory]
    [InlineData("/", "<title>QueenZone</title>", "The complete fan resource for Queen")]
    [InlineData("/news", "<title>QueenZone news</title>", "The latest Queen news")]
    [InlineData("/articles", "<title>QueenZone articles</title>", "In-depth Queen articles")]
    [InlineData("/photography", "<title>Photography | QueenZone</title>", "Browse Queen photograph collections")]
    [InlineData("/fan-performances", "<title>Fan Performances | QueenZone</title>", "Fan recordings of Queen songs")]
    [InlineData("/discography", "<title>Discography | QueenZone</title>", "Every Queen studio album")]
    [InlineData("/forum", "<title>Forum | QueenZone</title>", "Read-only Queenzone forum archive")]
    [InlineData("/biography", "<title>QueenZone biography</title>", "The story of Queen")]
    [InlineData("/timeline", "<title>Queen History Timeline", "Five decades of Queen history")]
    public async Task PublicPage_HasExpectedTitleAndDescription(string path, string expectedTitleTag, string expectedDescriptionFragment)
    {
        var client = factory.CreateClient();

        var body = await client.GetStringAsync(path);

        Assert.Contains(expectedTitleTag, body);
        Assert.Contains($"<meta name=\"description\" content=\"", body);
        Assert.Contains(expectedDescriptionFragment, body);
    }
}
