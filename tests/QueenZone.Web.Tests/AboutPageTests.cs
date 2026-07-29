using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace QueenZone.Web.Tests;

public sealed class AboutPageTests : IClassFixture<QueenZoneWebApplicationFactory>
{
    private readonly QueenZoneWebApplicationFactory factory;

    public AboutPageTests(QueenZoneWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task AboutPageRendersArchiveContext()
    {
        var client = factory.CreateClient();

        var body = await client.GetStringAsync("/about");

        Assert.Contains("The restored archive of Queenzone.com", body);
        Assert.Contains("Richard", body);
        Assert.Contains("Queen Page", body);
        Assert.Contains("www.richardorchard.com", body);
        Assert.Contains("href=\"https://www.richardorchard.com\"", body);
    }
}
