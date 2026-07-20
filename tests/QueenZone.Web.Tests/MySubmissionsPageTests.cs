using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using QueenZone.Data;
using QueenZone.Storage;
using QueenZone.Web;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace QueenZone.Web.Tests;

public sealed partial class MySubmissionsPageTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;
    private readonly InMemoryBlobStorageBackend blobBackend = new();

    public MySubmissionsPageTests(WebApplicationFactory<Program> factory)
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

                services.RemoveAll<IBlobUploadService>();
                services.AddSingleton<IBlobUploadService>(_ =>
                    new AzureBlobUploadService(blobBackend, Options.Create(new BlobUploadOptions())));
            });
        });
    }

    [Fact]
    public async Task Get_RedirectsUnauthenticatedUsersToLogin()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/account/my-submissions");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/account/login", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Get_ShowsOnlyCurrentMembersSubmissions_AcrossTabs()
    {
        var owner = await CreateSignedInMemberClientAsync(
            email: "mysubs-owner@example.com",
            displayName: "Owner Fan",
            subject: "google-mysubs-owner",
            options: new WebApplicationFactoryClientOptions
            {
                HandleCookies = true,
                AllowAutoRedirect = false,
            });

        var other = await CreateSignedInMemberClientAsync(
            email: "mysubs-other@example.com",
            displayName: "Other Fan",
            subject: "google-mysubs-other",
            options: new WebApplicationFactoryClientOptions
            {
                HandleCookies = true,
                AllowAutoRedirect = false,
            });

        await SubmitPhotoAsync(owner, "Owner exclusive photo");
        await SubmitNewsAsync(owner, "https://example.com/owner-exclusive-news-story", "Owner news");
        await SubmitArticleAsync(owner, "Owner exclusive article");

        await SubmitPhotoAsync(other, "Other member photo secret");
        await SubmitNewsAsync(other, "https://example.com/other-member-news-secret", "Other news");
        await SubmitArticleAsync(other, "Other member article secret");

        var ownerPhotos = await owner.GetStringAsync("/account/my-submissions?tab=photos");
        Assert.Contains("Owner exclusive photo", ownerPhotos);
        Assert.DoesNotContain("Other member photo secret", ownerPhotos);
        Assert.Contains("qz-status-badge", ownerPhotos);

        var ownerNews = await owner.GetStringAsync("/account/my-submissions?tab=news");
        Assert.Contains("owner-exclusive-news-story", ownerNews);
        Assert.DoesNotContain("other-member-news-secret", ownerNews);

        var ownerArticles = await owner.GetStringAsync("/account/my-submissions?tab=articles");
        Assert.Contains("Owner exclusive article", ownerArticles);
        Assert.Contains("Continue editing", ownerArticles);
        Assert.DoesNotContain("Other member article secret", ownerArticles);

        var otherPhotos = await other.GetStringAsync("/account/my-submissions?tab=photos");
        Assert.Contains("Other member photo secret", otherPhotos);
        Assert.DoesNotContain("Owner exclusive photo", otherPhotos);
    }

    [Fact]
    public async Task Get_ArticleDraft_LinksToSubmitArticleIdPath()
    {
        var client = await CreateSignedInMemberClientAsync(
            email: "mysubs-draft-link@example.com",
            displayName: "Draft Link Fan",
            subject: "google-mysubs-draft-link",
            options: new WebApplicationFactoryClientOptions
            {
                HandleCookies = true,
                AllowAutoRedirect = false,
            });

        var draftId = await SubmitArticleAsync(client, "Draft link target article");
        var page = await client.GetStringAsync("/account/my-submissions?tab=articles");

        Assert.Contains($"/submit/article/{draftId:D}", page);
        Assert.Contains("Continue editing", page);

        var editPage = await client.GetStringAsync($"/submit/article/{draftId:D}");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/submit/article/{draftId:D}")).StatusCode);
        Assert.Contains("Draft link target article", editPage);
    }

    private async Task SubmitPhotoAsync(HttpClient client, string title)
    {
        var formPage = await client.GetStringAsync("/submit/photo");
        await using var png = await CreatePngAsync();
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(ExtractAntiforgeryToken(formPage)), "__RequestVerificationToken");
        content.Add(new StringContent(title), "Title");
        content.Add(new StringContent("Queen"), "SuggestedCategory");
        var fileContent = new StreamContent(png);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "PhotoFile", "shot.png");

        var response = await client.PostAsync("/submit/photo", content);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    private async Task SubmitNewsAsync(HttpClient client, string url, string title)
    {
        var formPage = await client.GetStringAsync("/submit/news");
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = ExtractAntiforgeryToken(formPage),
            ["StoryUrl"] = url,
            ["Title"] = title,
            ["Notes"] = "Notes",
        });

        var response = await client.PostAsync("/submit/news", content);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    private async Task<Guid> SubmitArticleAsync(HttpClient client, string title)
    {
        var formPage = await client.GetStringAsync("/submit/article");
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = ExtractAntiforgeryToken(formPage),
            ["Title"] = title,
            ["Excerpt"] = "Excerpt",
            ["Body"] = new string('a', EfArticleSubmissionRepository.MinBodyVisibleChars),
            ["Tags"] = "queen",
            ["action"] = "save",
        });

        var response = await client.PostAsync("/submit/article", content);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var match = DraftIdRegex().Match(body);
        Assert.True(match.Success, "Draft id was not found after save.");
        return Guid.Parse(match.Groups["id"].Value);
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

    private static async Task<MemoryStream> CreatePngAsync()
    {
        using var image = new Image<Rgba32>(80, 60, new Rgba32(40, 120, 200));
        var stream = new MemoryStream();
        await image.SaveAsPngAsync(stream);
        stream.Position = 0;
        return stream;
    }

    private static string ExtractAntiforgeryToken(string html)
    {
        var match = AntiforgeryTokenRegex().Match(html);
        Assert.True(match.Success, "Antiforgery token was not found in the form.");
        return match.Groups["token"].Value;
    }

    [GeneratedRegex("""name="__RequestVerificationToken" value="(?<token>[^"]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex AntiforgeryTokenRegex();

    [GeneratedRegex("""name="DraftId"[^>]*value="(?<id>[^"]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex DraftIdRegex();
}
