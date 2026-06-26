using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace QueenZone.Web.Tests;

public sealed class AbsoluteCanonicalUrlTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public AbsoluteCanonicalUrlTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Site:PublicBaseUrl"] = "https://preview.queenzone.test"
                });
            });
        });
    }

    [Fact]
    public async Task NewsArchiveUsesConfiguredPublicBaseUrlForCanonicalAndPaginationLinks()
    {
        var client = factory.CreateClient();

        var pageOne = await client.GetStringAsync("/news");
        var pageTwo = await client.GetStringAsync("/news/page/2");

        Assert.Contains(
            """<link rel="canonical" href="https://preview.queenzone.test/news">""",
            pageOne);
        Assert.Contains(
            """<link rel="canonical" href="https://preview.queenzone.test/news/page/2">""",
            pageTwo);
        Assert.Contains(
            """<link rel="next" href="https://preview.queenzone.test/news/page/2">""",
            pageOne);
        Assert.Contains(
            """<link rel="prev" href="https://preview.queenzone.test/news">""",
            pageTwo);
    }
}