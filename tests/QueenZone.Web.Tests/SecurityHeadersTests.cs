using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace QueenZone.Web.Tests;

public sealed class SecurityHeadersTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public SecurityHeadersTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/news")]
    [InlineData("/account/login")]
    public async Task Response_HasXContentTypeOptionsNosniff(string path)
    {
        var response = await factory.CreateClient().GetAsync(path);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").First());
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/news")]
    [InlineData("/account/login")]
    public async Task Response_HasXFrameOptionsDeny(string path)
    {
        var response = await factory.CreateClient().GetAsync(path);
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").First());
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/news")]
    [InlineData("/account/login")]
    public async Task Response_HasReferrerPolicyStrictOriginWhenCrossOrigin(string path)
    {
        var response = await factory.CreateClient().GetAsync(path);
        Assert.Equal("strict-origin-when-cross-origin", response.Headers.GetValues("Referrer-Policy").First());
    }
}
