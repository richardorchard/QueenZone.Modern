using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace QueenZone.Web.Tests;

public sealed partial class AccountSettingsPageTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public AccountSettingsPageTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                services
                    .AddAuthentication()
                    .AddScheme<AuthenticationSchemeOptions, ExternalCookieTestHandler>(
                        MemberAuthenticationSchemes.ExternalCookie, _ => { });
            });
        });
    }

    [Fact]
    public async Task Get_RedirectsUnauthenticatedUsersToLogin()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/account/settings");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/account/login", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Get_ShowsPrefilledForm_ForAuthenticatedMember()
    {
        var client = await CreateSignedInMemberClientAsync(
            email: "settings-get@example.com",
            displayName: "Settings Fan",
            subject: "google-settings-get");

        var body = await client.GetStringAsync("/account/settings");

        Assert.Contains("Account settings", body);
        Assert.Contains("settings-get@example.com", body);
        Assert.Contains("value=\"Settings Fan\"", body);
        Assert.Contains("Google", body);
        Assert.Contains("name=\"DisplayName\"", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Save display name", body);
    }

    [Fact]
    public async Task Post_ValidName_SucceedsAndReissuesCookieWithUpdatedClaim()
    {
        var client = await CreateSignedInMemberClientAsync(
            email: "settings-post@example.com",
            displayName: "Before Name",
            subject: "google-settings-post",
            options: new WebApplicationFactoryClientOptions
            {
                HandleCookies = true,
                AllowAutoRedirect = false,
            });

        var formPage = await client.GetStringAsync("/account/settings");
        Assert.Contains("value=\"Before Name\"", formPage);

        var response = await client.PostAsync(
            "/account/settings",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiforgeryToken(formPage),
                ["DisplayName"] = "  After Name  ",
            }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/account/settings", response.Headers.Location!.OriginalString);

        // Follow redirect; header should show the new display name from the re-issued cookie.
        var updatedPage = await client.GetStringAsync("/account/settings");
        Assert.Contains("Display name updated.", updatedPage);
        Assert.Contains("value=\"After Name\"", updatedPage);
        Assert.Contains("After Name", updatedPage);
        Assert.DoesNotContain("Before Name", updatedPage);
    }

    [Fact]
    public async Task Post_InvalidName_ReturnsValidationErrors()
    {
        var client = await CreateSignedInMemberClientAsync(
            email: "settings-invalid@example.com",
            displayName: "Valid Name",
            subject: "google-settings-invalid",
            options: new WebApplicationFactoryClientOptions
            {
                HandleCookies = true,
                AllowAutoRedirect = false,
            });

        var formPage = await client.GetStringAsync("/account/settings");
        var response = await client.PostAsync(
            "/account/settings",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiforgeryToken(formPage),
                ["DisplayName"] = " ",
            }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("required", body, StringComparison.OrdinalIgnoreCase);
        // Form value retained (empty after trim may show empty input; original name should not be silently reapplied as success).
        Assert.DoesNotContain("Display name updated.", body);
    }

    [Fact]
    public async Task SignedInHeader_IncludesSettingsLink()
    {
        var client = await CreateSignedInMemberClientAsync(
            email: "settings-nav@example.com",
            displayName: "Nav Fan",
            subject: "google-settings-nav");

        var body = await client.GetStringAsync("/fan-performances");

        Assert.Contains("href=\"/account/settings\"", body);
        Assert.Contains(">Settings</a>", body);
    }

    private async Task<HttpClient> CreateSignedInMemberClientAsync(
        string email,
        string displayName,
        string subject,
        WebApplicationFactoryClientOptions? options = null)
    {
        var client = factory.CreateClient(options ?? new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = true,
        });
        client.DefaultRequestHeaders.Add(ExternalCookieTestHandler.ProviderHeader, "Google");
        client.DefaultRequestHeaders.Add(ExternalCookieTestHandler.SubjectHeader, subject);
        client.DefaultRequestHeaders.Add(ExternalCookieTestHandler.EmailHeader, email);
        client.DefaultRequestHeaders.Add(ExternalCookieTestHandler.NameHeader, displayName);

        var callbackResponse = await client.GetAsync("/account/external-login-callback");
        Assert.True(
            callbackResponse.StatusCode is HttpStatusCode.OK or HttpStatusCode.Redirect,
            $"Unexpected callback status code: {callbackResponse.StatusCode}");
        if (callbackResponse.StatusCode == HttpStatusCode.Redirect)
        {
            Assert.DoesNotContain("/account/login", callbackResponse.Headers.Location!.OriginalString);
        }

        return client;
    }

    private static string ExtractAntiforgeryToken(string html)
    {
        var match = AntiforgeryTokenRegex().Match(html);
        Assert.True(match.Success, "Antiforgery token was not found in the form.");
        return match.Groups["token"].Value;
    }

    [GeneratedRegex("""name="__RequestVerificationToken" value="(?<token>[^"]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex AntiforgeryTokenRegex();
}
