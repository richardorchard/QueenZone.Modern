using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QueenZone.Data;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed partial class AdminNewsRoutesTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string AdminEmail = "admin@test.local";
    private readonly WebApplicationFactory<Program> factory;

    public AdminNewsRoutesTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
    }

    [Fact]
    public async Task AnonymousUserCannotAccessAdminRoutes()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/admin/news");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthenticatedNonAdminCannotAccessAdminRoutes()
    {
        var client = CreateClient("stranger@example.com");

        var response = await client.GetAsync("/admin/news");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminRootRedirectsToNewsAdmin()
    {
        var client = CreateClient(AdminEmail);

        var response = await client.GetAsync("/admin");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/admin/news", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task AuthorizedAdminCanCreatePreviewPublishAndUnpublishArticle()
    {
        var store = new SharedNewsStore();
        var client = CreateClient(AdminEmail, store);

        var createResponse = await PostArticleAsync(
            client,
            "/admin/news/new",
            "/admin/news",
            new Dictionary<string, string>
            {
                ["title"] = "Admin created article",
                ["excerpt"] = "Created from the admin workflow.",
                ["body"] = "Plain text body for the new article.",
                ["publishedAt"] = "2026-06-14"
            });

        Assert.Equal(HttpStatusCode.Redirect, createResponse.StatusCode);
        var editPath = createResponse.Headers.Location!.OriginalString;
        Assert.Matches("/admin/news/\\d+/edit", editPath);

        var articleId = int.Parse(editPath.Split('/')[3], System.Globalization.CultureInfo.InvariantCulture);

        var previewBody = await client.GetStringAsync($"/admin/news/{articleId}/preview");
        Assert.Contains("Admin created article", previewBody);
        Assert.Contains("Plain text body for the new article.", previewBody);
        Assert.Contains("This draft is hidden from the public archive.", previewBody);

        var publicBeforePublish = await client.GetAsync("/news");
        var publicBodyBeforePublish = await publicBeforePublish.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Admin created article", publicBodyBeforePublish);

        var publishResponse = await PostActionAsync(client, $"/admin/news/{articleId}/publish");
        Assert.Equal(HttpStatusCode.Redirect, publishResponse.StatusCode);

        var publicBodyAfterPublish = await client.GetStringAsync("/news");
        Assert.Contains("Admin created article", publicBodyAfterPublish);
        Assert.Contains($"/news/{articleId}/admin-created-article", publicBodyAfterPublish);

        var unpublishResponse = await PostActionAsync(client, $"/admin/news/{articleId}/unpublish");
        Assert.Equal(HttpStatusCode.Redirect, unpublishResponse.StatusCode);

        var publicBodyAfterUnpublish = await client.GetStringAsync("/news");
        Assert.DoesNotContain("Admin created article", publicBodyAfterUnpublish);
    }

    [Fact]
    public async Task ValidationFailuresAreReturnedForInvalidDraft()
    {
        var store = new SharedNewsStore();
        var client = CreateClient(AdminEmail, store);

        var response = await PostArticleAsync(
            client,
            "/admin/news/new",
            "/admin/news",
            new Dictionary<string, string>
            {
                ["title"] = "",
                ["excerpt"] = "",
                ["body"] = "<script>alert(1)</script>",
                ["publishedAt"] = "",
                ["sourceUrl"] = "javascript:alert(1)"
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Title is required.", body);
        Assert.Contains("Excerpt is required.", body);
        Assert.Contains("Article body must be plain text.", body);
        Assert.Contains("Publication date is required.", body);
        Assert.Contains("Source URL must be a safe http or https link.", body);
    }

    [Fact]
    public async Task DuplicateSlugIsRejected()
    {
        var store = new SharedNewsStore(
        [
            new AdminNewsArticle(
                2001,
                "Existing article",
                "shared-slug",
                "Existing excerpt",
                "Existing body",
                new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                null,
                false,
                DateTime.UtcNow,
                DateTime.UtcNow,
                AdminEmail)
        ]);

        var client = CreateClient(AdminEmail, store);

        var response = await PostArticleAsync(
            client,
            "/admin/news/new",
            "/admin/news",
            new Dictionary<string, string>
            {
                ["title"] = "Another article",
                ["slug"] = "shared-slug",
                ["excerpt"] = "Another excerpt",
                ["body"] = "Another body",
                ["publishedAt"] = "2026-06-15"
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Slug is already in use by another article.", body);
    }

    [Fact]
    public async Task AuthorizedAdminCanDeleteArticle()
    {
        var store = new SharedNewsStore(
        [
            new AdminNewsArticle(
                3001,
                "Delete me",
                "delete-me",
                "Delete excerpt",
                "Delete body",
                new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                null,
                false,
                DateTime.UtcNow,
                DateTime.UtcNow,
                AdminEmail)
        ]);

        var client = CreateClient(AdminEmail, store);

        var deleteResponse = await PostActionAsync(client, "/admin/news/3001/delete");
        Assert.Equal(HttpStatusCode.Redirect, deleteResponse.StatusCode);

        var listResponse = await client.GetAsync("/admin/news");
        var listBody = await listResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Delete me", listBody);
    }

    [Fact]
    public async Task PublishAndEditActionsAreAudited()
    {
        var store = new SharedNewsStore();
        var appFactory = CreateFactory(store);
        var client = CreateClientFromFactory(appFactory, AdminEmail);

        var createResponse = await PostArticleAsync(
            client,
            "/admin/news/new",
            "/admin/news",
            new Dictionary<string, string>
            {
                ["title"] = "Audited article",
                ["excerpt"] = "Audit excerpt",
                ["body"] = "Audit body",
                ["publishedAt"] = "2026-06-14"
            });

        var editPath = createResponse.Headers.Location!.OriginalString;
        var articleId = int.Parse(editPath.Split('/')[3], System.Globalization.CultureInfo.InvariantCulture);

        await using var scope = appFactory.Services.CreateAsyncScope();
        var auditRepository = scope.ServiceProvider.GetRequiredService<INewsAuditRepository>();
        var createAudit = await auditRepository.GetByNewsIdAsync(articleId);
        Assert.Contains(createAudit, entry => entry.Action == "create");

        await PostActionAsync(client, $"/admin/news/{articleId}/publish");

        var publishAudit = await auditRepository.GetByNewsIdAsync(articleId);
        Assert.Contains(publishAudit, entry => entry.Action == "publish");
        Assert.Contains(publishAudit, entry => entry.ActorEmail == AdminEmail);
    }

    private WebApplicationFactory<Program> CreateFactory(SharedNewsStore store) =>
        factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<SharedNewsStore>();
                services.RemoveAll<INewsRepository>();
                services.RemoveAll<IAdminNewsRepository>();
                services.RemoveAll<INewsAuditRepository>();
                services.AddSingleton(store);
                services.AddSingleton<INewsRepository>(_ => new QueenZone.Data.InMemoryNewsRepository(store));
                services.AddSingleton<IAdminNewsRepository>(_ => new InMemoryAdminNewsRepository(store));
                services.AddSingleton<INewsAuditRepository>(_ => new InMemoryNewsAuditRepository(store));
            }));

    private HttpClient CreateClient(string? email = null, SharedNewsStore? store = null)
    {
        var appFactory = store is null ? factory : CreateFactory(store);
        return CreateClientFromFactory(appFactory, email);
    }

    private static HttpClient CreateClientFromFactory(WebApplicationFactory<Program> appFactory, string? email)
    {
        var client = appFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });

        if (!string.IsNullOrWhiteSpace(email))
        {
            client.DefaultRequestHeaders.Add(TestAuthHandler.UserEmailHeader, email);
        }

        return client;
    }

    private static async Task<HttpResponseMessage> PostArticleAsync(
        HttpClient client,
        string formPath,
        string postPath,
        Dictionary<string, string> fields)
    {
        var formPage = await client.GetStringAsync(formPath);
        fields[AdminNewsRoutes.AntiforgeryTokenFieldName] = ExtractAntiforgeryToken(formPage);
        return await client.PostAsync(postPath, new FormUrlEncodedContent(fields));
    }

    private static async Task<HttpResponseMessage> PostActionAsync(HttpClient client, string actionPath)
    {
        var listPage = await client.GetStringAsync("/admin/news");
        var token = ExtractAntiforgeryToken(listPage);
        return await client.PostAsync(actionPath, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            [AdminNewsRoutes.AntiforgeryTokenFieldName] = token
        }));
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
