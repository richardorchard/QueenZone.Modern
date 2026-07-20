using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace QueenZone.Web.Tests;

public sealed class FanPerformanceRateLimitTests
{
    private WebApplicationFactory<Program> CreateFactory(
        int audioPermitLimit = 10,
        int browsePermitLimit = 60) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                services.Configure<FanPerformanceRateLimitingOptions>(opts =>
                {
                    opts.AudioPermitLimit = audioPermitLimit;
                    opts.AudioSlidingWindowSeconds = 3600;
                    opts.BrowsePermitLimit = browsePermitLimit;
                    opts.BrowseWindowSeconds = 3600;
                });

                services
                    .AddAuthentication()
                    .AddScheme<AuthenticationSchemeOptions, ExternalCookieTestHandler>(
                        MemberAuthenticationSchemes.ExternalCookie, _ => { });
            });
        });

    [Fact]
    public async Task AudioEndpoint_Returns429_AfterPermitLimitExceeded()
    {
        await using var factory = CreateFactory(audioPermitLimit: 1);
        var client = await CreateSignedInMemberClientAsync(factory);

        var first = await client.GetAsync("/fan-performances/187/audio");
        var second = await client.GetAsync("/fan-performances/187/audio");

        Assert.Equal(HttpStatusCode.Redirect, first.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, second.StatusCode);
    }

    [Fact]
    public async Task AudioEndpoint_AnonymousVisitor_StillRedirectsToLogin_WhenLimitExceeded()
    {
        await using var factory = CreateFactory(audioPermitLimit: 1);
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var first = await client.GetAsync("/fan-performances/187/audio");
        var second = await client.GetAsync("/fan-performances/187/audio");

        // Anonymous users are caught by auth before reaching the rate limiter,
        // so both requests redirect to login regardless of the permit count.
        Assert.Equal(HttpStatusCode.Redirect, first.StatusCode);
        Assert.Contains("/account/login", first.Headers.Location!.OriginalString);
        Assert.Equal(HttpStatusCode.Redirect, second.StatusCode);
        Assert.Contains("/account/login", second.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task BrowsePage_Returns429_AfterPermitLimitExceeded()
    {
        await using var factory = CreateFactory(browsePermitLimit: 1);
        var client = factory.CreateClient();

        var first = await client.GetAsync("/fan-performances");
        var second = await client.GetAsync("/fan-performances");

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, second.StatusCode);
    }

    [Fact]
    public async Task BrowsePage_AllowsRequestsWithinLimit()
    {
        await using var factory = CreateFactory(browsePermitLimit: 5);
        var client = factory.CreateClient();

        for (var i = 0; i < 5; i++)
        {
            var response = await client.GetAsync("/fan-performances");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    private static async Task<HttpClient> CreateSignedInMemberClientAsync(
        WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false,
        });
        client.DefaultRequestHeaders.Add(ExternalCookieTestHandler.ProviderHeader, "Google");
        client.DefaultRequestHeaders.Add(ExternalCookieTestHandler.SubjectHeader, "google-rate-limit-test-subject");
        client.DefaultRequestHeaders.Add(ExternalCookieTestHandler.EmailHeader, "ratelimit@example.com");
        client.DefaultRequestHeaders.Add(ExternalCookieTestHandler.NameHeader, "Rate Limit Test Member");

        var callbackResponse = await client.GetAsync("/account/external-login-callback");
        Assert.Equal(HttpStatusCode.Redirect, callbackResponse.StatusCode);
        Assert.DoesNotContain("/account/login", callbackResponse.Headers.Location!.OriginalString);

        return client;
    }
}
