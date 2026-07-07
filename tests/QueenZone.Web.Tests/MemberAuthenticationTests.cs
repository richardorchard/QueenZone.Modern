using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace QueenZone.Web.Tests;

public sealed class MemberAuthenticationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public MemberAuthenticationTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
    }

    [Fact]
    public async Task AnonymousUserCannotAccessMemberProbe()
    {
        // Cookie auth challenges with a 302 redirect to the login page rather than a bare 401,
        // so don't auto-follow the redirect — assert the challenge itself.
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/account/member-probe");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/account/login", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task AdminTestHeaderAloneDoesNotGrantMemberAccess()
    {
        // The Admin allowlist scheme ("Test") and the Member policy's scheme ("MembersCookie")
        // are deliberately separate auth schemes, so being an authenticated admin user does not
        // implicitly grant member access.
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserEmailHeader, "admin@test.local");

        var response = await client.GetAsync("/account/member-probe");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/account/login", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task LoginPageRenders()
    {
        var client = factory.CreateClient();

        var body = await client.GetStringAsync("/account/login");

        Assert.Contains("Sign in", body);
        Assert.Contains("Sign in to QueenZone", body);
    }

    [Fact]
    public async Task AnonymousHeaderRendersMobileLoginAction()
    {
        var client = factory.CreateClient();

        var body = await client.GetStringAsync("/");

        Assert.Contains("class=\"qz-mobile-account\"", body);
        Assert.Contains("class=\"qz-masthead__signin qz-masthead__signin--mobile\" href=\"/account/login\"", body);
    }

}
