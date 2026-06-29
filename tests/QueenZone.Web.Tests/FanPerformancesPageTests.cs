using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace QueenZone.Web.Tests;

public sealed class FanPerformancesPageTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public FanPerformancesPageTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
    }

    [Fact]
    public async Task FanPerformancesPageRendersSeedPerformances()
    {
        var client = factory.CreateClient();

        var body = await client.GetStringAsync("/fan-performances");

        Assert.Contains("Fan Performances", body);
        Assert.Contains("Reaching Out", body);
        Assert.Contains("Mike Ryde", body);
        Assert.Contains("https://pictures.queenzone.org/songfiles/2014417798057369.mp3", body);
    }

    [Fact]
    public async Task FanPerformancesPageTwo_RedirectsToIndex_WhenOnlyOnePageOfSeedData()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/fan-performances/page/2");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }
}
