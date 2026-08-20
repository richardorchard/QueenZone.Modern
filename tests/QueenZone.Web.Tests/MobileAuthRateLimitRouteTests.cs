using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace QueenZone.Web.Tests;

public sealed class MobileAuthRateLimitRouteTests
{
    [Fact]
    public async Task Authorize_ReturnsRfc6749TooManyRequests_AfterIpLimit()
    {
        using var factory = CreateFactory(ipPermitLimit: 1);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var pair = MobileAuthPkceTestData.CreatePair();

        var first = await client.GetAsync(AuthorizeUrl(pair.Challenge));
        var second = await client.GetAsync(AuthorizeUrl(pair.Challenge));

        Assert.Equal(HttpStatusCode.Redirect, first.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, second.StatusCode);
        Assert.Equal("application/json", second.Content.Headers.ContentType?.MediaType);
        var payload = await second.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("temporarily_unavailable", payload.GetProperty("error").GetString());
        Assert.DoesNotContain("code_challenge", await second.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Refresh_ReturnsRfc6749TooManyRequests_AfterAccountLimit()
    {
        using var factory = CreateFactory(accountPermitLimit: 1);
        var issued = await CompletePkceAsync(factory);

        using var refreshRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = MobileAuthOptions.DefaultClientId,
            ["refresh_token"] = issued.RefreshToken,
        });
        var refresh = await issued.Client.PostAsync(MobileAuthEndpoints.TokenPath, refreshRequest);

        Assert.Equal(HttpStatusCode.TooManyRequests, refresh.StatusCode);
        var body = await refresh.Content.ReadAsStringAsync();
        Assert.Contains("temporarily_unavailable", body, StringComparison.Ordinal);
        Assert.DoesNotContain(issued.RefreshToken, body, StringComparison.Ordinal);
        Assert.DoesNotContain(issued.AccessToken, body, StringComparison.Ordinal);
    }

    private static QueenZoneWebApplicationFactory CreateFactory(
        int ipPermitLimit = 30,
        int accountPermitLimit = 10) =>
        QueenZoneWebApplicationFactory.WithServices(services =>
        {
            services.Configure<AuthRateLimitingOptions>(opts =>
            {
                opts.IpPermitLimit = ipPermitLimit;
                opts.IpWindowMinutes = 60;
                opts.AccountPermitLimit = accountPermitLimit;
                opts.AccountWindowMinutes = 60;
            });

            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, ExternalCookieTestHandler>(
                    MemberAuthenticationSchemes.ExternalCookie, _ => { });

            foreach (var provider in MemberAuthenticationSchemes.ExternalProviders)
            {
                services.AddAuthentication()
                    .AddScheme<AuthenticationSchemeOptions, TestOAuthProviderHandler>(provider, _ => { });
            }
        });

    private static async Task<(HttpClient Client, string AccessToken, string RefreshToken)> CompletePkceAsync(
        QueenZoneWebApplicationFactory factory)
    {
        var pair = MobileAuthPkceTestData.CreatePair();
        const string state = "rate-limit-state";
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });
        client.DefaultRequestHeaders.Add(ExternalCookieTestHandler.ProviderHeader, MemberAuthenticationSchemes.Google);
        client.DefaultRequestHeaders.Add(ExternalCookieTestHandler.SubjectHeader, "google-rate-limit-auth");
        client.DefaultRequestHeaders.Add(ExternalCookieTestHandler.EmailHeader, "auth-rate@example.com");
        client.DefaultRequestHeaders.Add(ExternalCookieTestHandler.NameHeader, "Auth Rate Fan");

        var authorize = await client.GetAsync(AuthorizeUrl(pair.Challenge, state));
        var callback = await client.GetAsync(authorize.Headers.Location!);
        var query = QueryHelpers.ParseQuery(callback.Headers.Location!.Query);
        using var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = MobileAuthOptions.DefaultClientId,
            ["redirect_uri"] = MobileAuthPkceTestData.RedirectUri,
            ["code"] = query["code"].ToString(),
            ["code_verifier"] = pair.Verifier,
        });
        var tokenResponse = await client.PostAsync(MobileAuthEndpoints.TokenPath, tokenRequest);
        var payload = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>();
        return (
            client,
            payload.GetProperty("access_token").GetString()!,
            payload.GetProperty("refresh_token").GetString()!);
    }

    private static string AuthorizeUrl(string challenge, string state = "st") =>
        $"{MobileAuthEndpoints.AuthorizePath}?response_type=code" +
        $"&client_id={MobileAuthOptions.DefaultClientId}" +
        $"&redirect_uri={Uri.EscapeDataString(MobileAuthPkceTestData.RedirectUri)}" +
        $"&code_challenge={challenge}&code_challenge_method=S256" +
        $"&state={Uri.EscapeDataString(state)}&provider=Google";
}
