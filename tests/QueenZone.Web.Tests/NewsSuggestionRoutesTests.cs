using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using QueenZone.Data;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed partial class NewsSuggestionRoutesTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string AdminEmail = "admin@test.local";
    private readonly WebApplicationFactory<Program> factory;

    public NewsSuggestionRoutesTests(WebApplicationFactory<Program> factory)
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
    public async Task Get_SubmitNews_RedirectsUnauthenticatedUsersToLogin()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/submit/news");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/account/login", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Post_ValidSubmission_CreatesPendingRow()
    {
        var client = await CreateSignedInMemberClientAsync(
            email: "news-submit@example.com",
            displayName: "News Fan",
            subject: "google-news-submit",
            options: new WebApplicationFactoryClientOptions
            {
                HandleCookies = true,
                AllowAutoRedirect = false,
            });

        var formPage = await client.GetStringAsync("/submit/news");
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = ExtractAntiforgeryToken(formPage),
            ["StoryUrl"] = "https://example.com/queen-tour-announcement",
            ["Title"] = "Tour announced",
            ["Notes"] = "Big story for the site",
        });

        var response = await client.PostAsync("/submit/news", content);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/submit/news/confirmation", response.Headers.Location!.OriginalString);

        var confirmation = await client.GetStringAsync("/submit/news/confirmation");
        Assert.Contains("Thank you for the suggestion!", confirmation);

        var repository = factory.Services.GetRequiredService<INewsSuggestionRepository>();
        var memberId = await GetMemberIdForEmailAsync("news-submit@example.com");
        var suggestions = await repository.GetPendingAsync(1, 10);
        var suggestion = Assert.Single(suggestions, item => item.Url.Contains("queen-tour-announcement", StringComparison.Ordinal));
        Assert.Equal(NewsSuggestionStatus.Pending, suggestion.Status);
        Assert.Equal("Tour announced", suggestion.Title);

        var stored = await repository.GetByIdAsync(suggestion.Id);
        Assert.NotNull(stored);
        Assert.Equal(memberId, stored!.SubmitterMemberId);
    }

    [Fact]
    public async Task Post_DuplicateUrl_ReturnsDuplicateMessage_WithoutCreatingNewRow()
    {
        var client = await CreateSignedInMemberClientAsync(
            email: "news-dup@example.com",
            displayName: "Dup Fan",
            subject: "google-news-dup",
            options: new WebApplicationFactoryClientOptions
            {
                HandleCookies = true,
                AllowAutoRedirect = false,
            });

        var formPage = await client.GetStringAsync("/submit/news");
        async Task<HttpResponseMessage> SubmitAsync(string formHtml, string urlSuffix)
        {
            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiforgeryToken(formHtml),
                ["StoryUrl"] = $"https://example.com/shared-story{urlSuffix}",
                ["Title"] = "Shared story",
            });
            return await client.PostAsync("/submit/news", content);
        }

        var first = await SubmitAsync(formPage, "");
        Assert.Equal(HttpStatusCode.Redirect, first.StatusCode);

        formPage = await client.GetStringAsync("/submit/news");
        var second = await SubmitAsync(formPage, "/");
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var body = await second.Content.ReadAsStringAsync();
        Assert.Contains("already been suggested", body, StringComparison.OrdinalIgnoreCase);

        var repository = factory.Services.GetRequiredService<INewsSuggestionRepository>();
        var pending = await repository.GetPendingAsync(1, 20);
        Assert.Single(pending, item => item.Url.Contains("shared-story", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Get_AdminQueue_RequiresAdminAuthentication()
    {
        var anonymous = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/admin/news-suggestions")).StatusCode);

        var stranger = CreateAdminClient("stranger@example.com");
        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.GetAsync("/admin/news-suggestions")).StatusCode);

        var admin = CreateAdminClient(AdminEmail);
        var body = await admin.GetStringAsync("/admin/news-suggestions");
        Assert.Contains("Member news suggestions", body);
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
        var match = AntiforgeryTokenRegex().Match(html);
        Assert.True(match.Success, "Antiforgery token was not found in the form.");
        return match.Groups["token"].Value;
    }

    [GeneratedRegex("""name="__RequestVerificationToken" value="(?<token>[^"]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex AntiforgeryTokenRegex();
}
