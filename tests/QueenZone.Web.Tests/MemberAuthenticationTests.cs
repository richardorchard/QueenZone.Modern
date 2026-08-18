using System.Net;
using AspNet.Security.OAuth.Apple;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace QueenZone.Web.Tests;

public sealed class MemberAuthenticationTests : IClassFixture<QueenZoneWebApplicationFactory>
{
    private readonly QueenZoneWebApplicationFactory factory;

    public MemberAuthenticationTests(QueenZoneWebApplicationFactory factory)
    {
        this.factory = factory;
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
        Assert.DoesNotContain("Continue with Apple", body);
    }

    [Fact]
    public async Task LoginPageShowsAppleOnlyWhenFullyConfigured()
    {
        using var configuredFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Authentication:Apple:ClientId", "org.queenzone.web");
            builder.UseSetting("Authentication:Apple:TeamId", "TEAM123456");
            builder.UseSetting("Authentication:Apple:KeyId", "KEY1234567");
            builder.UseSetting("Authentication:Apple:PrivateKey", "test-private-key");
        });
        var client = configuredFactory.CreateClient();

        var body = await client.GetStringAsync("/account/login");

        Assert.Contains("Continue with Apple", body);
        Assert.Contains("provider=Apple", body);
    }

    [Fact]
    public async Task AppleLoginStartsAppleAuthorizationFlow()
    {
        using var configuredFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Authentication:Apple:ClientId", "org.queenzone.web");
            builder.UseSetting("Authentication:Apple:TeamId", "TEAM123456");
            builder.UseSetting("Authentication:Apple:KeyId", "KEY1234567");
            builder.UseSetting("Authentication:Apple:PrivateKey", "test-private-key");
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication().AddApple(MemberAuthenticationSchemes.Apple, options =>
                {
                    options.ClientId = "org.queenzone.web";
                    options.TeamId = "TEAM123456";
                    options.KeyId = "KEY1234567";
                    options.GenerateClientSecret = true;
                    options.PrivateKey = (_, _) =>
                        Task.FromResult<ReadOnlyMemory<char>>("test-private-key".AsMemory());
                });
            });
        });
        var client = configuredFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await client.GetAsync("/account/external-login?provider=Apple&returnUrl=%2Fforum");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("appleid.apple.com", response.Headers.Location?.Host);
        Assert.Contains("response_mode=form_post", response.Headers.Location?.Query);
        Assert.DoesNotContain("prompt=", response.Headers.Location?.Query);
    }

    [Fact]
    public async Task AnonymousHeaderRendersMobileLoginAction()
    {
        var client = factory.CreateClient();

        var body = await client.GetStringAsync("/");

        Assert.Contains("href=\"/account/login\"", body);
        Assert.Contains(">Member sign in<", body);
    }

    [Fact]
    public async Task AdminHeaderDistinguishesAdminAccessFromMemberSignIn()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserEmailHeader, "admin@test.local");

        var body = await client.GetStringAsync("/admin");

        Assert.Contains("Admin access: admin@test.local", body);
        Assert.Contains(">Member sign in<", body);
        Assert.DoesNotContain("Signed in as admin", body);
    }

}
