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

public sealed partial class ArticleSubmitRoutesTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string AdminEmail = "admin@test.local";
    private readonly WebApplicationFactory<Program> factory;

    public ArticleSubmitRoutesTests(WebApplicationFactory<Program> factory)
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
    public async Task GetSubmitArticle_RedirectsAnonymousUser()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await client.GetAsync("/submit/article");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/account/login", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task GetAdminArticles_Returns401_ForAnonymousUser()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await client.GetAsync("/admin/articles");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAdminArticles_Returns200_ForAdminUser()
    {
        var client = CreateAdminClient(AdminEmail);

        var response = await client.GetAsync("/admin/articles");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PostAutosave_Returns403_WithoutAntiforgery()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Title"] = "Test",
            ["Body"] = "Hello",
        });

        var response = await client.PostAsync("/submit/article/autosave", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Member_CanSaveDraftSubmitAndSeeConfirmation()
    {
        var client = await CreateSignedInMemberClientAsync(
            email: "article-submit@example.com",
            displayName: "Article Author",
            subject: "google-article-submit",
            options: new WebApplicationFactoryClientOptions
            {
                HandleCookies = true,
                AllowAutoRedirect = false,
            });

        var formPage = await client.GetStringAsync("/submit/article");
        using var saveContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = ExtractAntiforgeryToken(formPage),
            ["Title"] = "Wembley retrospective",
            ["Excerpt"] = "A look back at the 1986 show",
            ["Body"] = MinBody(),
            ["Tags"] = "Wembley, Live",
            ["action"] = "save",
        });

        var saveResponse = await client.PostAsync("/submit/article", saveContent);
        Assert.Equal(HttpStatusCode.OK, saveResponse.StatusCode);
        var formPageAfterSave = await saveResponse.Content.ReadAsStringAsync();
        Assert.Contains("Draft saved.", formPageAfterSave);

        var draftId = ExtractDraftId(formPageAfterSave);

        using var submitContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = ExtractAntiforgeryToken(formPageAfterSave),
            ["DraftId"] = draftId,
            ["Title"] = "Wembley retrospective",
            ["Excerpt"] = "A look back at the 1986 show",
            ["Body"] = MinBody(),
            ["Tags"] = "Wembley, Live",
            ["action"] = "submit",
        });

        var submitResponse = await client.PostAsync("/submit/article", submitContent);
        Assert.Equal(HttpStatusCode.Redirect, submitResponse.StatusCode);
        Assert.StartsWith("/submit/article/confirmation/", submitResponse.Headers.Location!.OriginalString);

        var confirmation = await client.GetStringAsync(submitResponse.Headers.Location!.OriginalString);
        Assert.Contains("Wembley retrospective", confirmation);
        Assert.Contains(ArticleSubmissionStatus.Submitted, confirmation);

        var mySubmissions = await client.GetStringAsync("/account/my-submissions");
        Assert.Contains("Wembley retrospective", mySubmissions);
        Assert.Contains(ArticleSubmissionStatus.Submitted, mySubmissions);
    }

    [Fact]
    public async Task PostSubmit_WithShortBody_ShowsValidationError()
    {
        var client = await CreateSignedInMemberClientAsync(
            email: "article-short@example.com",
            displayName: "Short Body",
            subject: "google-article-short",
            options: new WebApplicationFactoryClientOptions
            {
                HandleCookies = true,
                AllowAutoRedirect = false,
            });

        var formPage = await client.GetStringAsync("/submit/article");
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = ExtractAntiforgeryToken(formPage),
            ["Title"] = "Too short",
            ["Body"] = "Not enough text.",
            ["action"] = "submit",
        });

        var response = await client.PostAsync("/submit/article", content);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("300", body);
    }

    [Fact]
    public async Task PostAutosave_ReturnsDraftId_ForSignedInMember()
    {
        var client = await CreateSignedInMemberClientAsync(
            email: "article-autosave@example.com",
            displayName: "Autosave Author",
            subject: "google-article-autosave",
            options: new WebApplicationFactoryClientOptions
            {
                HandleCookies = true,
                AllowAutoRedirect = false,
            });

        var formPage = await client.GetStringAsync("/submit/article");
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = ExtractAntiforgeryToken(formPage),
            ["Title"] = "Autosaved draft",
            ["Body"] = "Draft body text.",
        });

        var response = await client.PostAsync("/submit/article/autosave", content);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("draftId", body);
    }

    [Fact]
    public async Task Admin_CanReviewApprovePublishAndReject()
    {
        var memberClient = await CreateSignedInMemberClientAsync(
            email: "article-admin-flow@example.com",
            displayName: "Admin Flow Author",
            subject: "google-article-admin-flow",
            options: new WebApplicationFactoryClientOptions
            {
                HandleCookies = true,
                AllowAutoRedirect = false,
            });

        var publishId = await SubmitArticleAsync(memberClient, "Publish story", "publish-story");
        var rejectId = await SubmitArticleAsync(memberClient, "Reject story", "reject-story");
        var reviseId = await SubmitArticleAsync(memberClient, "Revise story", "revise-story");

        var admin = CreateAdminClient(AdminEmail);

        var queue = await admin.GetStringAsync("/admin/articles");
        Assert.Contains("Publish story", queue);
        Assert.Contains("Admin Flow Author", queue);

        var detail = await admin.GetStringAsync($"/admin/articles/{publishId}");
        Assert.Contains("Publish story", detail);

        await PostAdminActionAsync(admin, publishId, new Dictionary<string, string>
        {
            ["submitAction"] = "underreview",
            ["ReviewNotes"] = "Starting review",
        });

        var repository = factory.Services.GetRequiredService<IArticleSubmissionRepository>();
        Assert.Equal(ArticleSubmissionStatus.UnderReview, (await repository.GetByIdAsync(publishId))!.Status);

        await PostAdminActionAsync(admin, publishId, new Dictionary<string, string>
        {
            ["submitAction"] = "approve",
            ["Slug"] = "publish-story",
            ["ReviewNotes"] = "Approved",
        });
        Assert.Equal(ArticleSubmissionStatus.ApprovedForPublishing, (await repository.GetByIdAsync(publishId))!.Status);

        var publishResponse = await PostAdminActionAsync(admin, publishId, new Dictionary<string, string>
        {
            ["submitAction"] = "publish",
            ["Slug"] = "publish-story",
        });
        Assert.Equal(HttpStatusCode.Redirect, publishResponse.StatusCode);
        Assert.Equal(ArticleSubmissionStatus.Published, (await repository.GetByIdAsync(publishId))!.Status);

        await PostAdminActionAsync(admin, reviseId, new Dictionary<string, string>
        {
            ["submitAction"] = "revise",
            ["RejectionReason"] = "Needs more detail",
            ["ReviewNotes"] = "Expand the outro",
        });

        var rejectWithoutReason = await PostAdminActionAsync(admin, rejectId, new Dictionary<string, string>
        {
            ["submitAction"] = "reject",
        });
        Assert.Equal(HttpStatusCode.Redirect, rejectWithoutReason.StatusCode);

        await PostAdminActionAsync(admin, rejectId, new Dictionary<string, string>
        {
            ["submitAction"] = "reject",
            ["RejectionReason"] = "Off topic",
        });

        Assert.Equal(ArticleSubmissionStatus.RequiresRevision, (await repository.GetByIdAsync(reviseId))!.Status);
        Assert.Equal(ArticleSubmissionStatus.Rejected, (await repository.GetByIdAsync(rejectId))!.Status);

        var articlesIndex = await factory.CreateClient().GetStringAsync("/articles");
        Assert.Contains("Publish story", articlesIndex);
        Assert.Contains("Community article", articlesIndex);

        var publicDetail = await factory.CreateClient().GetStringAsync("/articles/publish-story");
        Assert.Contains("Publish story", publicDetail);
    }

    [Fact]
    public async Task GetEditDraft_LoadsExistingDraftForMember()
    {
        var client = await CreateSignedInMemberClientAsync(
            email: "article-edit@example.com",
            displayName: "Edit Author",
            subject: "google-article-edit",
            options: new WebApplicationFactoryClientOptions
            {
                HandleCookies = true,
                AllowAutoRedirect = false,
            });

        var draftId = await SaveDraftAsync(client, "Editable draft", "Draft body content.");

        var editPage = await client.GetStringAsync($"/submit/article?handler=Edit&id={draftId:D}");
        Assert.Contains("Editable draft", editPage);
        Assert.Contains("Edit draft", editPage);
    }

    [Fact]
    public async Task ArticlesIndexRendersSuccessfully()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/articles");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Articles", body);
    }

    [Fact]
    public async Task CommunityDetail_Returns404_WhenSlugNotFound()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await client.GetAsync("/articles/nonexistent-community-slug-xyz");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AdminDetail_Returns404_ForUnknownSubmission()
    {
        var admin = CreateAdminClient(AdminEmail);
        var response = await admin.GetAsync($"/admin/articles/{Guid.NewGuid():D}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<Guid> SubmitArticleAsync(HttpClient client, string title, string slug)
    {
        var draftId = await SaveDraftAsync(client, title, MinBody());
        var formPage = await client.GetStringAsync("/submit/article");
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = ExtractAntiforgeryToken(formPage),
            ["DraftId"] = draftId.ToString("D"),
            ["Title"] = title,
            ["Body"] = MinBody(),
            ["action"] = "submit",
        });

        var response = await client.PostAsync("/submit/article", content);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var repository = factory.Services.GetRequiredService<IArticleSubmissionRepository>();
        var pending = await repository.GetPendingAsync(1, 50);
        return pending.Single(item => item.Title == title).Id;
    }

    private async Task<Guid> SaveDraftAsync(HttpClient client, string title, string body)
    {
        var formPage = await client.GetStringAsync("/submit/article");
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = ExtractAntiforgeryToken(formPage),
            ["Title"] = title,
            ["Body"] = body,
            ["action"] = "save",
        });

        var response = await client.PostAsync("/submit/article", content);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        return Guid.Parse(ExtractDraftId(html));
    }

    private async Task<HttpResponseMessage> PostAdminActionAsync(
        HttpClient client,
        Guid id,
        Dictionary<string, string> fields)
    {
        var detail = await client.GetStringAsync($"/admin/articles/{id:D}");
        var form = new Dictionary<string, string>(fields)
        {
            ["__RequestVerificationToken"] = ExtractAntiforgeryToken(detail),
            ["Slug"] = fields.GetValueOrDefault("Slug") ?? "article-slug",
        };
        return await client.PostAsync($"/admin/articles/{id:D}/action", new FormUrlEncodedContent(form));
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

    private static string MinBody() => new('x', EfArticleSubmissionRepository.MinBodyVisibleChars);

    private static string ExtractAntiforgeryToken(string html)
    {
        var match = AntiforgeryTokenRegex().Match(html);
        Assert.True(match.Success, "Antiforgery token was not found in the form.");
        return match.Groups["token"].Value;
    }

    private static string ExtractDraftId(string html)
    {
        var match = DraftIdRegex().Match(html);
        Assert.True(match.Success, "Draft ID was not found in the form.");
        return match.Groups["id"].Value;
    }

    [GeneratedRegex("""name="__RequestVerificationToken"[^>]*value="(?<token>[^"]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex AntiforgeryTokenRegex();

    [GeneratedRegex("""name="DraftId"[^>]*value="(?<id>[^"]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex DraftIdRegex();
}
