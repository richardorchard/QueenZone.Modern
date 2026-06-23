using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace QueenZone.Web.Tests;

public sealed class ForumPageTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public ForumPageTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
    }

    [Fact]
    public async Task ForumPageRendersSeedCategories()
    {
        var client = factory.CreateClient();

        var body = await client.GetStringAsync("/forum");

        Assert.Contains("Forum", body);
        Assert.Contains("The Music", body);
        Assert.Contains("/forum/1/the-music", body);
        Assert.Contains("<strong>6</strong> boards", body);
    }
}