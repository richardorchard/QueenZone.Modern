using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using QueenZone.Data;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class AdminMembersRoutesTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string AdminEmail = "admin@test.local";
    private readonly WebApplicationFactory<Program> factory;

    public AdminMembersRoutesTests(WebApplicationFactory<Program> factory)
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
    public async Task Get_AdminMembers_RequiresAdminAuthentication()
    {
        var anonymous = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/admin/members")).StatusCode);

        var stranger = CreateAdminClient("stranger@example.com");
        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.GetAsync("/admin/members")).StatusCode);

        var admin = CreateAdminClient(AdminEmail);
        var body = await admin.GetStringAsync("/admin/members");
        Assert.Contains("Members", body);
    }

    [Fact]
    public async Task Get_AdminMembers_SearchFiltersByDisplayNameOrEmail()
    {
        await CreateSignedInMemberClientAsync("spammer@example.com", "Spam Bot", "google-spam-search");
        await CreateSignedInMemberClientAsync("regular@example.com", "Regular Fan", "google-regular-search");

        var admin = CreateAdminClient(AdminEmail);
        var body = await admin.GetStringAsync("/admin/members?query=Spam");

        Assert.Contains("Spam Bot", body);
        Assert.DoesNotContain("Regular Fan", body);
    }

    [Fact]
    public async Task Suspend_RequiresReason()
    {
        var memberClient = await CreateSignedInMemberClientAsync(
            "no-reason@example.com", "No Reason Fan", "google-no-reason");
        var memberId = await GetMemberIdForEmailAsync("no-reason@example.com");

        var admin = CreateAdminClient(AdminEmail);
        var detail = await admin.GetStringAsync($"/admin/members/{memberId}");
        var response = await admin.PostAsync(
            $"/admin/members/{memberId}/Suspend",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiforgeryToken(detail),
            }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var members = factory.Services.GetRequiredService<IMemberAccountRepository>();
        var account = await members.FindByIdAsync(memberId);
        Assert.False(account!.IsSuspended);

        _ = memberClient;
    }

    [Fact]
    public async Task Suspend_BlocksFutureSignIn_AndEndsExistingSession_AndReinstateRestoresAccess()
    {
        const string email = "board-spammer@example.com";
        var memberClient = await CreateSignedInMemberClientAsync(email, "Board Spammer", "google-board-spammer");
        var memberId = await GetMemberIdForEmailAsync(email);

        // The member's existing session works before suspension.
        var beforeSuspension = await memberClient.GetAsync("/account/settings");
        Assert.Equal(HttpStatusCode.OK, beforeSuspension.StatusCode);

        var admin = CreateAdminClient(AdminEmail);
        var detail = await admin.GetStringAsync($"/admin/members/{memberId}");
        var suspendResponse = await admin.PostAsync(
            $"/admin/members/{memberId}/Suspend",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiforgeryToken(detail),
                ["Reason"] = "Posting spam links across the forum",
            }));
        Assert.Equal(HttpStatusCode.Redirect, suspendResponse.StatusCode);

        var members = factory.Services.GetRequiredService<IMemberAccountRepository>();
        var suspended = await members.FindByIdAsync(memberId);
        Assert.True(suspended!.IsSuspended);
        Assert.Equal("Posting spam links across the forum", suspended.SuspendedReason);
        Assert.Equal(AdminEmail, suspended.SuspendedByAdminEmail);

        // The member's existing cookie session is rejected on their very next request.
        var afterSuspensionOptions = new HttpRequestMessage(HttpMethod.Get, "/account/settings");
        var afterSuspension = await memberClient.SendAsync(afterSuspensionOptions);
        Assert.NotEqual(HttpStatusCode.OK, afterSuspension.StatusCode);

        // A fresh OAuth sign-in attempt is also rejected.
        var retryClient = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false,
        });
        retryClient.DefaultRequestHeaders.Add(ExternalCookieTestHandler.ProviderHeader, "Google");
        retryClient.DefaultRequestHeaders.Add(ExternalCookieTestHandler.SubjectHeader, "google-board-spammer");
        retryClient.DefaultRequestHeaders.Add(ExternalCookieTestHandler.EmailHeader, email);
        retryClient.DefaultRequestHeaders.Add(ExternalCookieTestHandler.NameHeader, "Board Spammer");
        var retryCallback = await retryClient.GetAsync("/account/external-login-callback");
        Assert.Equal(HttpStatusCode.Redirect, retryCallback.StatusCode);
        Assert.Contains("suspended=1", retryCallback.Headers.Location!.OriginalString);

        // Reinstating restores sign-in access.
        var reinstateDetail = await admin.GetStringAsync($"/admin/members/{memberId}");
        var reinstateResponse = await admin.PostAsync(
            $"/admin/members/{memberId}/Reinstate",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiforgeryToken(reinstateDetail),
            }));
        Assert.Equal(HttpStatusCode.Redirect, reinstateResponse.StatusCode);

        var reinstated = await members.FindByIdAsync(memberId);
        Assert.False(reinstated!.IsSuspended);

        var freshSignIn = await CreateSignedInMemberClientAsync(email, "Board Spammer", "google-board-spammer");
        var settingsAfterReinstate = await freshSignIn.GetAsync("/account/settings");
        Assert.Equal(HttpStatusCode.OK, settingsAfterReinstate.StatusCode);
    }

    private HttpClient CreateAdminClient(string? email = null)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        if (!string.IsNullOrWhiteSpace(email))
        {
            client.DefaultRequestHeaders.Add(TestAuthHandler.UserEmailHeader, email);
        }

        return client;
    }

    private async Task<HttpClient> CreateSignedInMemberClientAsync(string email, string displayName, string subject)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false,
        });
        client.DefaultRequestHeaders.Add(ExternalCookieTestHandler.ProviderHeader, "Google");
        client.DefaultRequestHeaders.Add(ExternalCookieTestHandler.SubjectHeader, subject);
        client.DefaultRequestHeaders.Add(ExternalCookieTestHandler.EmailHeader, email);
        client.DefaultRequestHeaders.Add(ExternalCookieTestHandler.NameHeader, displayName);

        var callbackResponse = await client.GetAsync("/account/external-login-callback");
        Assert.True(
            callbackResponse.StatusCode is HttpStatusCode.OK or HttpStatusCode.Redirect,
            $"Unexpected callback status code: {callbackResponse.StatusCode}");

        return client;
    }

    private async Task<Guid> GetMemberIdForEmailAsync(string email)
    {
        var members = factory.Services.GetRequiredService<IMemberAccountRepository>();
        var account = await members.FindByEmailAsync(email);
        Assert.NotNull(account);
        return account!.Id;
    }

    private static string ExtractAntiforgeryToken(string html)
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            html, """name="__RequestVerificationToken"[^>]*value="(?<token>[^"]+)""");
        Assert.True(match.Success, "Antiforgery token was not found in the form.");
        return match.Groups["token"].Value;
    }
}
