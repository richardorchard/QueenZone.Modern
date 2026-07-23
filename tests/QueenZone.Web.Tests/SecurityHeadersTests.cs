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

    [Theory]
    [InlineData("/")]
    [InlineData("/news")]
    [InlineData("/account/login")]
    public async Task Response_HasPermissionsPolicy(string path)
    {
        var response = await factory.CreateClient().GetAsync(path);
        Assert.Equal(
            SecurityHeaders.PermissionsPolicy,
            response.Headers.GetValues("Permissions-Policy").First());
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/news")]
    [InlineData("/account/login")]
    public async Task Response_HasContentSecurityPolicyReportOnly(string path)
    {
        var response = await factory.CreateClient().GetAsync(path);
        var csp = response.Headers.GetValues("Content-Security-Policy-Report-Only").First();
        Assert.Equal(SecurityHeaders.ContentSecurityPolicyReportOnly, csp);
        Assert.Contains("default-src 'self'", csp, StringComparison.Ordinal);
        Assert.Contains("frame-ancestors 'none'", csp, StringComparison.Ordinal);
    }
}
