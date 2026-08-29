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

    [Fact]
    public async Task Suspend_HidesMembersForumPosts_AndReinstateRestoresThem()
    {
        const string email = "post-spammer@example.com";
        await CreateSignedInMemberClientAsync(email, "Post Spammer", "google-post-spammer");
        var memberId = await GetMemberIdForEmailAsync(email);

        var forumWriteRepository = factory.Services.GetRequiredService<IForumWriteRepository>();
        await forumWriteRepository.CreateThreadAsync(new NewForumThread(
            1,
            memberId,
            "Post Spammer",
            "Buy cheap watches here",
            "<p>Click this link for cheap watches</p>",
            DateTimeOffset.UtcNow));

        var profileClient = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var beforeSuspend = await profileClient.GetStringAsync($"/members/{memberId}");
        Assert.Contains("Buy cheap watches here", beforeSuspend);
        var topicsBeforeSuspend = await profileClient.GetStringAsync("/api/v1/forum/categories/1/topics");
        Assert.Contains("Buy cheap watches here", topicsBeforeSuspend);

        var admin = CreateAdminClient(AdminEmail);
        var detail = await admin.GetStringAsync($"/admin/members/{memberId}");
        var suspendResponse = await admin.PostAsync(
            $"/admin/members/{memberId}/Suspend",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiforgeryToken(detail),
                ["Reason"] = "Posting spam links",
            }));
        Assert.Equal(HttpStatusCode.Redirect, suspendResponse.StatusCode);

        var afterSuspend = await profileClient.GetStringAsync($"/members/{memberId}");
        Assert.DoesNotContain("Buy cheap watches here", afterSuspend);
        var topicsAfterSuspend = await profileClient.GetStringAsync("/api/v1/forum/categories/1/topics");
        Assert.DoesNotContain("Buy cheap watches here", topicsAfterSuspend);

        var reinstateDetail = await admin.GetStringAsync($"/admin/members/{memberId}");
        var reinstateResponse = await admin.PostAsync(
            $"/admin/members/{memberId}/Reinstate",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiforgeryToken(reinstateDetail),
            }));
        Assert.Equal(HttpStatusCode.Redirect, reinstateResponse.StatusCode);

        var afterReinstate = await profileClient.GetStringAsync($"/members/{memberId}");
        Assert.Contains("Buy cheap watches here", afterReinstate);
        var topicsAfterReinstate = await profileClient.GetStringAsync("/api/v1/forum/categories/1/topics");
        Assert.Contains("Buy cheap watches here", topicsAfterReinstate);
    }

    [Fact]
    public async Task HideForumContent_HidesPostsWithoutSuspending_AndUnhideRestoresThem()
    {
        const string email = "hide-only@example.com";
        await CreateSignedInMemberClientAsync(email, "Hide Only", "google-hide-only");
        var memberId = await GetMemberIdForEmailAsync(email);

        var forumWriteRepository = factory.Services.GetRequiredService<IForumWriteRepository>();
        await forumWriteRepository.CreateThreadAsync(new NewForumThread(
            1,
            memberId,
            "Hide Only",
            "Hide this topic",
            "<p>Spam body</p>",
            DateTimeOffset.UtcNow));

        var admin = CreateAdminClient(AdminEmail);
        var detail = await admin.GetStringAsync($"/admin/members/{memberId}");
        Assert.Contains("Hide all posts and threads", detail);
        Assert.Contains("1 post(s) and 1 thread(s)", detail);

        var hideResponse = await admin.PostAsync(
            $"/admin/members/{memberId}/HideForumContent",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiforgeryToken(detail),
            }));
        Assert.Equal(HttpStatusCode.Redirect, hideResponse.StatusCode);

        var members = factory.Services.GetRequiredService<IMemberAccountRepository>();
        var account = await members.FindByIdAsync(memberId);
        Assert.False(account!.IsSuspended);

        var profileClient = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        Assert.DoesNotContain("Hide this topic", await profileClient.GetStringAsync($"/members/{memberId}"));
        Assert.DoesNotContain(
            "Hide this topic",
            await profileClient.GetStringAsync("/api/v1/forum/categories/1/topics"));

        var hiddenDetail = await admin.GetStringAsync($"/admin/members/{memberId}");
        Assert.Contains("Unhide posts and threads", hiddenDetail);
        var unhideResponse = await admin.PostAsync(
            $"/admin/members/{memberId}/UnhideForumContent",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiforgeryToken(hiddenDetail),
            }));
        Assert.Equal(HttpStatusCode.Redirect, unhideResponse.StatusCode);

        Assert.Contains("Hide this topic", await profileClient.GetStringAsync($"/members/{memberId}"));
        Assert.Contains(
            "Hide this topic",
            await profileClient.GetStringAsync("/api/v1/forum/categories/1/topics"));
    }

    [Fact]
    public async Task Search_ShowsNoAccountAuthor_AndHideUnhideWorks()
    {
        var write = factory.Services.GetRequiredService<IForumWriteRepository>() as InMemoryForumWriteRepository
            ?? throw new InvalidOperationException("Expected in-memory forum write repository in Testing.");
        write.SeedUnlinkedThread(
            1,
            "PatriciaCMardis",
            "Unlinked spam topic",
            "<p>Buy followers</p>",
            DateTimeOffset.UtcNow);

        var admin = CreateAdminClient(AdminEmail);
        var search = await admin.GetStringAsync("/admin/members?query=PatriciaCMardis");
        Assert.Contains("PatriciaCMardis", search);
        Assert.Contains("No member account", search);
        Assert.Contains("Hide all posts and threads", search);

        var hideResponse = await admin.PostAsync(
            "/admin/members/author/HideForumContent",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiforgeryToken(search),
                ["DisplayName"] = "PatriciaCMardis",
            }));
        Assert.Equal(HttpStatusCode.Redirect, hideResponse.StatusCode);

        var profileClient = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        Assert.DoesNotContain(
            "Unlinked spam topic",
            await profileClient.GetStringAsync("/api/v1/forum/categories/1/topics"));

        var hiddenSearch = await admin.GetStringAsync("/admin/members?query=PatriciaCMardis");
        Assert.Contains("Unhide posts and threads", hiddenSearch);
        var unhideResponse = await admin.PostAsync(
            "/admin/members/author/UnhideForumContent",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiforgeryToken(hiddenSearch),
                ["DisplayName"] = "PatriciaCMardis",
            }));
        Assert.Equal(HttpStatusCode.Redirect, unhideResponse.StatusCode);

        Assert.Contains(
            "Unlinked spam topic",
            await profileClient.GetStringAsync("/api/v1/forum/categories/1/topics"));
    }

    [Fact]
    public async Task HideForumContent_RequiresAdminAuthentication()
    {
        const string email = "hide-auth@example.com";
        await CreateSignedInMemberClientAsync(email, "Hide Auth", "google-hide-auth");
        var memberId = await GetMemberIdForEmailAsync(email);
        var admin = CreateAdminClient(AdminEmail);
        var detail = await admin.GetStringAsync($"/admin/members/{memberId}");
        var token = ExtractAntiforgeryToken(detail);

        var stranger = CreateAdminClient("stranger@example.com");
        var forbidden = await stranger.PostAsync(
            $"/admin/members/{memberId}/HideForumContent",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
            }));
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        var anonymous = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var unauthorized = await anonymous.PostAsync(
            $"/admin/members/{memberId}/HideForumContent",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
            }));
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);
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
