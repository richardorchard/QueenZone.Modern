using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace QueenZone.Web.Tests;

public sealed class ExternalLoginCallbackTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public ExternalLoginCallbackTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                // ExternalCookie is not registered in Testing mode (the Testing branch only adds
                // MembersCookie). Register it here with a test double that reads claims from
                // request headers instead of a real OAuth-backed cookie, so tests can drive
                // ExternalLoginCallback without a live OAuth round-trip.
                services
                    .AddAuthentication()
                    .AddScheme<AuthenticationSchemeOptions, ExternalCookieTestHandler>(
                        MemberAuthenticationSchemes.ExternalCookie, _ => { });
            });
        });
    }

    [Fact]
    public async Task Callback_WithoutExternalCookie_RedirectsToLogin()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/account/external-login-callback");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/account/login", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Callback_WithValidExternalCookie_SignsInAndGrantsMemberAccess()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false,
        });
        // These headers are read by ExternalCookieTestHandler to simulate what the OAuth
        // provider's redirect leaves behind in the ExternalCookie scheme.
        client.DefaultRequestHeaders.Add(ExternalCookieTestHandler.ProviderHeader, "Google");
        client.DefaultRequestHeaders.Add(ExternalCookieTestHandler.SubjectHeader, "google-subject-42");
        client.DefaultRequestHeaders.Add(ExternalCookieTestHandler.EmailHeader, "googlefan@example.com");
        client.DefaultRequestHeaders.Add(ExternalCookieTestHandler.NameHeader, "Google Fan");

        var callbackResponse = await client.GetAsync("/account/external-login-callback");

        // Successful sign-in redirects away from the callback page (to "/" or returnUrl), not to login.
        Assert.Equal(HttpStatusCode.Redirect, callbackResponse.StatusCode);
        Assert.DoesNotContain("/account/login", callbackResponse.Headers.Location!.OriginalString);

        // MembersCookie is now stored in the cookie jar; member-probe should be accessible.
        var probeResponse = await client.GetAsync("/account/member-probe");
        Assert.Equal(HttpStatusCode.OK, probeResponse.StatusCode);
    }

    [Fact]
    public async Task Callback_WithAllowedAdminEmail_GrantsAdminAccess()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false,
        });
        client.DefaultRequestHeaders.Add(ExternalCookieTestHandler.ProviderHeader, "Google");
        client.DefaultRequestHeaders.Add(ExternalCookieTestHandler.SubjectHeader, "google-admin-subject");
        client.DefaultRequestHeaders.Add(ExternalCookieTestHandler.EmailHeader, AdminHttpTestHelpers.AdminEmail);
        client.DefaultRequestHeaders.Add(ExternalCookieTestHandler.NameHeader, "Test Admin");

        var callbackResponse = await client.GetAsync("/account/external-login-callback");
        Assert.Equal(HttpStatusCode.Redirect, callbackResponse.StatusCode);

        var adminResponse = await client.GetAsync("/admin/news");
        Assert.Equal(HttpStatusCode.OK, adminResponse.StatusCode);
    }

    [Fact]
    public async Task Callback_WithValidExternalCookie_HonoursSafeReturnUrl()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add(ExternalCookieTestHandler.ProviderHeader, "Google");
        client.DefaultRequestHeaders.Add(ExternalCookieTestHandler.SubjectHeader, "google-subject-99");
        client.DefaultRequestHeaders.Add(ExternalCookieTestHandler.EmailHeader, "returntest@example.com");
        client.DefaultRequestHeaders.Add(ExternalCookieTestHandler.NameHeader, "Return Test");

        var response = await client.GetAsync("/account/external-login-callback?returnUrl=%2Fforum");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/forum", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Callback_WithValidExternalCookie_RejectsAbsoluteReturnUrl()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add(ExternalCookieTestHandler.ProviderHeader, "Google");
        client.DefaultRequestHeaders.Add(ExternalCookieTestHandler.SubjectHeader, "google-subject-66");
        client.DefaultRequestHeaders.Add(ExternalCookieTestHandler.EmailHeader, "openredirect@example.com");
        client.DefaultRequestHeaders.Add(ExternalCookieTestHandler.NameHeader, "Open Redirect Attempt");

        // An absolute URL must be rejected by the open-redirect guard and fall back to "/".
        var response = await client.GetAsync("/account/external-login-callback?returnUrl=https%3A%2F%2Fevil.example.com");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location!.OriginalString);
    }
}

/// <summary>
/// Test double for the ExternalCookie authentication scheme. Reads claims from request
/// headers instead of a real OAuth-backed cookie, so integration tests can drive
/// ExternalLoginCallback without a live OAuth provider.
/// Extends SignOutAuthenticationHandler so that SignOutAsync (called by the callback page
/// to clean up the external cookie after sign-in) succeeds without a real cookie store.
/// </summary>
internal sealed class ExternalCookieTestHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : SignOutAuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string ProviderHeader = "X-Test-External-Provider";
    public const string SubjectHeader = "X-Test-External-Subject";
    public const string EmailHeader = "X-Test-External-Email";
    public const string NameHeader = "X-Test-External-Name";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(EmailHeader, out var emailValues))
            return Task.FromResult(AuthenticateResult.NoResult());

        var provider = Request.Headers[ProviderHeader].FirstOrDefault() ?? "Google";
        var subject = Request.Headers[SubjectHeader].FirstOrDefault() ?? "test-subject";
        var email = emailValues.First()!;
        var name = Request.Headers[NameHeader].FirstOrDefault() ?? email;

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, subject),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, name),
        };
        // AuthenticationType becomes the provider name read by ExternalLoginCallback.
        var identity = new ClaimsIdentity(claims, authenticationType: provider);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    // No-op: the real ExternalCookie removes its cookie from the browser, but in tests
    // there is no real cookie jar entry to clean up.
    protected override Task HandleSignOutAsync(AuthenticationProperties? properties) =>
        Task.CompletedTask;
}
