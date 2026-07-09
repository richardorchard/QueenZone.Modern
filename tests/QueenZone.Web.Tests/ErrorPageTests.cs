using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace QueenZone.Web.Tests;

public sealed class ErrorPageTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public ErrorPageTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
    }

    [Fact]
    public async Task MissingRouteRendersCustomNotFoundPage()
    {
        var response = await factory.CreateClient().GetAsync("/missing-archive-page");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("Page Not Found", body);
        Assert.Contains("Find it in the archive", body);
        Assert.Contains("href=\"/search\"", body);
    }

    [Fact]
    public async Task ErrorRouteRendersCustomServerErrorPage()
    {
        var response = await factory.CreateClient().GetAsync("/error");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("Something went wrong", body);
        Assert.Contains("Keep browsing the archive", body);
        Assert.Contains("Error reference:", body);
    }

    [Fact]
    public async Task ErrorRouteCanRenderStatusSpecificPage()
    {
        var response = await factory.CreateClient().GetAsync("/error/403");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("Access denied", body);
        Assert.Contains("This area is not available", body);
    }

}
