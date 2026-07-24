using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QueenZone.Data;
using QueenZone.Web;
using QueenZone.Web.Pages.Admin.News;

namespace QueenZone.Web.Tests;

[Collection(AdminNewsDeleteErrorCollection.Name)]
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
    public async Task AdminRootRendersDashboard()
    {
        var client = CreateClient(AdminEmail);

        var response = await client.GetAsync("/admin");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Dashboard", body);
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
                ["body"] = "<p>Rich text body is now allowed</p>",
                ["publishedAt"] = "",
                ["sourceUrl"] = "javascript:alert(1)"
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Title is required.", body);
        Assert.Contains("Excerpt is required.", body);
        Assert.DoesNotContain("Article body must be plain text.", body);
        Assert.Contains("Publication date is required.", body);
        Assert.Contains("Source URL must be a safe http or https link.", body);
    }

    [Fact]
    public async Task HtmlBodyIsSavedAndRenderedAsHtml()
    {
        var store = new SharedNewsStore();
        var client = CreateClient(AdminEmail, store);

        var createResponse = await PostArticleAsync(
            client,
            "/admin/news/new",
            "/admin/news",
            new Dictionary<string, string>
            {
                ["title"] = "Rich text article",
                ["excerpt"] = "Article with bold text.",
                ["body"] = "<p><strong>bold content</strong></p>",
                ["publishedAt"] = "2026-06-14"
            });

        Assert.Equal(HttpStatusCode.Redirect, createResponse.StatusCode);
        Assert.DoesNotContain("Article body must be plain text.", createResponse.Headers.Location?.OriginalString ?? string.Empty);

        var editPath = createResponse.Headers.Location!.OriginalString;
        var articleId = int.Parse(editPath.Split('/')[3], System.Globalization.CultureInfo.InvariantCulture);

        var editBody = await client.GetStringAsync($"/admin/news/{articleId}/edit");
        Assert.DoesNotContain("Article body must be plain text.", editBody);

        var publishResponse = await PostActionAsync(client, $"/admin/news/{articleId}/publish");
        Assert.Equal(HttpStatusCode.Redirect, publishResponse.StatusCode);

        var detailSlug = "rich-text-article";
        var detailBody = await client.GetStringAsync($"/news/{articleId}/{detailSlug}");
        Assert.Contains("<strong>bold content</strong>", detailBody);
        Assert.DoesNotContain("&lt;strong&gt;", detailBody);
    }

    [Fact]
    public async Task CreatePostWithoutAntiforgeryTokenReturnsBadRequest()
    {
        var client = CreateClient(AdminEmail, new SharedNewsStore());

        var response = await client.PostAsync(
            "/admin/news",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["title"] = "Missing token article",
                ["excerpt"] = "Missing token excerpt",
                ["body"] = "Missing token body",
                ["publishedAt"] = "2026-06-14"
            }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
    public async Task DuplicateSlugOnEditIsRejected()
    {
        var store = new SharedNewsStore(
        [
            new AdminNewsArticle(
                2002,
                "Existing article",
                "shared-edit-slug",
                "Existing excerpt",
                "Existing body",
                new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                null,
                false,
                DateTime.UtcNow,
                DateTime.UtcNow,
                AdminEmail),
            new AdminNewsArticle(
                2003,
                "Editable article",
                "editable-slug",
                "Editable excerpt",
                "Editable body",
                new DateTime(2026, 6, 2, 0, 0, 0, DateTimeKind.Utc),
                null,
                false,
                DateTime.UtcNow,
                DateTime.UtcNow,
                AdminEmail)
        ]);

        var client = CreateClient(AdminEmail, store);

        var response = await PostArticleAsync(
            client,
            "/admin/news/2003/edit",
            "/admin/news/2003",
            new Dictionary<string, string>
            {
                ["title"] = "Editable article",
                ["slug"] = "shared-edit-slug",
                ["excerpt"] = "Editable excerpt",
                ["body"] = "Editable body",
                ["publishedAt"] = "2026-06-02"
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Slug is already in use by another article.", body);
    }

    [Fact]
    public async Task DuplicateSlugOnPublishIsRejected()
    {
        var store = new SharedNewsStore(
        [
            new AdminNewsArticle(
                2004,
                "Published collision owner",
                "publish-collision-slug",
                "Owner excerpt",
                "Owner body",
                new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                null,
                true,
                DateTime.UtcNow,
                DateTime.UtcNow,
                AdminEmail),
            new AdminNewsArticle(
                2005,
                "Draft with colliding slug",
                "publish-collision-slug",
                "Draft excerpt",
                "Draft body",
                new DateTime(2026, 6, 2, 0, 0, 0, DateTimeKind.Utc),
                null,
                false,
                DateTime.UtcNow,
                DateTime.UtcNow,
                AdminEmail)
        ]);

        var client = CreateClient(AdminEmail, store);

        var publishResponse = await PostActionAsync(client, "/admin/news/2005/publish");
        Assert.Equal(HttpStatusCode.Redirect, publishResponse.StatusCode);
        Assert.Equal("/admin/news/2005/edit", publishResponse.Headers.Location!.OriginalString);

        var editBody = await client.GetStringAsync("/admin/news/2005/edit");
        Assert.Contains("Slug is already in use by another article.", editBody);
        Assert.Contains("admin-status--error", editBody);
    }

    [Fact]
    public async Task AuthorizedAdminCanOpenEditPage()
    {
        var store = new SharedNewsStore(
        [
            new AdminNewsArticle(
                4001,
                "Editable article",
                "editable-article",
                "Editable excerpt",
                "Editable body",
                new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                null,
                false,
                DateTime.UtcNow,
                DateTime.UtcNow,
                AdminEmail)
        ]);

        var client = CreateClient(AdminEmail, store);

        var response = await client.GetAsync("/admin/news/4001/edit");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Edit article", body);
        Assert.Contains("Editable article", body);
        Assert.Contains("admin-form-panel", body);
        Assert.DoesNotContain("<h1>Edit: Editable article</h1>", body);
    }

    [Fact]
    public async Task AuthorizedAdminCanSaveEditedArticle()
    {
        var store = new SharedNewsStore(
        [
            new AdminNewsArticle(
                4002,
                "Before save",
                "before-save",
                "Original excerpt",
                "Original body",
                new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                null,
                false,
                DateTime.UtcNow,
                DateTime.UtcNow,
                AdminEmail)
        ]);
        var client = CreateClient(AdminEmail, store);

        var saveResponse = await PostArticleAsync(
            client,
            "/admin/news/4002/edit",
            "/admin/news/4002",
            new Dictionary<string, string>
            {
                ["title"] = "After save",
                ["slug"] = "after-save",
                ["excerpt"] = "Saved excerpt",
                ["body"] = "Saved body text",
                ["publishedAt"] = "2026-06-15"
            });

        Assert.Equal(HttpStatusCode.Redirect, saveResponse.StatusCode);
        Assert.Equal("/admin/news/4002/edit", saveResponse.Headers.Location!.OriginalString);

        var editBody = await client.GetStringAsync("/admin/news/4002/edit");
        Assert.Contains("After save", editBody);
        Assert.Contains("Saved body text", editBody);
        Assert.DoesNotContain("Original body", editBody);
    }

    [Fact]
    public async Task OverlongTitleIsRejected()
    {
        var store = new SharedNewsStore();
        var client = CreateClient(AdminEmail, store);

        var response = await PostArticleAsync(
            client,
            "/admin/news/new",
            "/admin/news",
            new Dictionary<string, string>
            {
                ["title"] = new string('x', NewsValidation.MaxTitleLength + 1),
                ["excerpt"] = "Excerpt",
                ["body"] = "Body",
                ["publishedAt"] = "2026-06-14"
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains($"Title must be {NewsValidation.MaxTitleLength} characters or fewer.", body);
    }

    [Fact]
    public async Task DeleteForeignKeyViolation_showsErrorMessageOnAdminList()
    {
        var store = new SharedNewsStore(
        [
            new AdminNewsArticle(
                3101,
                "Linked article",
                "linked-article",
                "Excerpt",
                "Body",
                new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                null,
                false,
                DateTime.UtcNow,
                DateTime.UtcNow,
                AdminEmail)
        ]);
        var discoveryStore = new SharedNewsDiscoveryStore();
        var discoveryInner = new InMemoryNewsDiscoveryRepository(discoveryStore);
        using var _ = AdminNewsDeleteError.UseForeignKeyViolationClassifier(_ => true);
        var client = CreateClient(
            AdminEmail,
            store,
            services =>
            {
                services.RemoveAll<IAdminNewsRepository>();
                services.AddSingleton<IAdminNewsRepository>(_ =>
                    new FailingDeleteAdminNewsRepository(
                        new InMemoryAdminNewsRepository(store),
                        new DbUpdateException("FK violation", new InvalidOperationException("blocked"))));
            },
            discoveryInner);

        var deleteResponse = await PostActionAsync(client, "/admin/news/3101/delete");
        Assert.Equal(HttpStatusCode.Redirect, deleteResponse.StatusCode);

        var listBody = await client.GetStringAsync(deleteResponse.Headers.Location!.OriginalString);
        Assert.Contains("could not be deleted", listBody, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("admin-status--error", listBody);
        Assert.Contains("Linked article", listBody);
    }

    [Fact]
    public async Task DeleteNotFound_showsErrorMessageOnAdminList()
    {
        var store = new SharedNewsStore(
        [
            new AdminNewsArticle(
                3102,
                "Missing on delete",
                "missing-on-delete",
                "Excerpt",
                "Body",
                new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                null,
                false,
                DateTime.UtcNow,
                DateTime.UtcNow,
                AdminEmail)
        ]);
        var discoveryStore = new SharedNewsDiscoveryStore();
        var discoveryInner = new InMemoryNewsDiscoveryRepository(discoveryStore);
        var client = CreateClient(
            AdminEmail,
            store,
            services =>
            {
                services.RemoveAll<IAdminNewsRepository>();
                services.AddSingleton<IAdminNewsRepository>(_ =>
                    new FailingDeleteAdminNewsRepository(
                        new InMemoryAdminNewsRepository(store),
                        new InvalidOperationException("News article 3102 was not found.")));
            },
            discoveryInner);

        var deleteResponse = await PostActionAsync(client, "/admin/news/3102/delete");
        Assert.Equal(HttpStatusCode.Redirect, deleteResponse.StatusCode);

        var listBody = await client.GetStringAsync(deleteResponse.Headers.Location!.OriginalString);
        Assert.Contains("News article 3102 was not found.", listBody);
        Assert.Contains("admin-status--error", listBody);
    }

    [Fact]
    public async Task Delete_continues_when_discovery_link_cleanup_fails()
    {
        var store = new SharedNewsStore(
        [
            new AdminNewsArticle(
                3103,
                "Cleanup failure article",
                "cleanup-failure",
                "Excerpt",
                "Body",
                new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                null,
                false,
                DateTime.UtcNow,
                DateTime.UtcNow,
                AdminEmail)
        ]);
        var discoveryStore = new SharedNewsDiscoveryStore();
        var discoveryInner = new InMemoryNewsDiscoveryRepository(discoveryStore);
        var client = CreateClient(
            AdminEmail,
            store,
            _ => { },
            new ConfigurableNewsDiscoveryRepository(discoveryInner)
            {
                ClearPromotedNewsLinksHandler = (_, _) =>
                    throw new InvalidOperationException("Discovery tables unavailable.")
            });

        var deleteResponse = await PostActionAsync(client, "/admin/news/3103/delete");
        Assert.Equal(HttpStatusCode.Redirect, deleteResponse.StatusCode);

        var listBody = await client.GetStringAsync("/admin/news");
        Assert.DoesNotContain("Cleanup failure article", listBody);
    }

    [Fact]
    public async Task EditPage_loads_when_provenance_lookup_fails()
    {
        var store = new SharedNewsStore(
        [
            new AdminNewsArticle(
                4101,
                "Provenance failure article",
                "provenance-failure",
                "Excerpt",
                "Body",
                new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                null,
                false,
                DateTime.UtcNow,
                DateTime.UtcNow,
                AdminEmail)
        ]);
        var discoveryStore = new SharedNewsDiscoveryStore();
        var discoveryInner = new InMemoryNewsDiscoveryRepository(discoveryStore);
        var client = CreateClient(
            AdminEmail,
            store,
            _ => { },
            new ConfigurableNewsDiscoveryRepository(discoveryInner)
            {
                GetCandidateByPromotedNewsIdHandler = (_, _) =>
                    throw new InvalidOperationException("Discovery lookup failed.")
            });

        var response = await client.GetAsync("/admin/news/4101/edit");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Edit article", body);
        Assert.Contains("Provenance failure article", body);
        Assert.DoesNotContain("Discovery provenance", body);
    }

    [Fact]
    public async Task EditPost_validation_rerenders_when_provenance_lookup_fails()
    {
        var store = new SharedNewsStore(
        [
            new AdminNewsArticle(
                4103,
                "Edit post provenance failure",
                "edit-post-provenance-failure",
                "Excerpt",
                "Body",
                new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                null,
                false,
                DateTime.UtcNow,
                DateTime.UtcNow,
                AdminEmail)
        ]);
        var discoveryStore = new SharedNewsDiscoveryStore();
        var discoveryInner = new InMemoryNewsDiscoveryRepository(discoveryStore);
        var client = CreateClient(
            AdminEmail,
            store,
            _ => { },
            new ConfigurableNewsDiscoveryRepository(discoveryInner)
            {
                GetCandidateByPromotedNewsIdHandler = (_, _) =>
                    throw new InvalidOperationException("Discovery lookup failed.")
            });

        var response = await PostArticleAsync(
            client,
            "/admin/news/4103/edit",
            "/admin/news/4103",
            new Dictionary<string, string>
            {
                ["title"] = "",
                ["excerpt"] = "Excerpt",
                ["body"] = "Body",
                ["publishedAt"] = "2026-06-14"
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Title is required.", body);
        Assert.Contains("Edit post provenance failure", body);
        Assert.DoesNotContain("Discovery provenance", body);
    }

    [Fact]
    public async Task PreviewPage_loads_when_provenance_lookup_fails()
    {
        var store = new SharedNewsStore(
        [
            new AdminNewsArticle(
                4102,
                "Preview provenance failure",
                "preview-provenance-failure",
                "Excerpt",
                "Body",
                new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                null,
                false,
                DateTime.UtcNow,
                DateTime.UtcNow,
                AdminEmail)
        ]);
        var discoveryStore = new SharedNewsDiscoveryStore();
        var discoveryInner = new InMemoryNewsDiscoveryRepository(discoveryStore);
        var client = CreateClient(
            AdminEmail,
            store,
            _ => { },
            new ConfigurableNewsDiscoveryRepository(discoveryInner)
            {
                GetCandidateByPromotedNewsIdHandler = (_, _) =>
                    throw new InvalidOperationException("Discovery lookup failed.")
            });

        var response = await client.GetAsync("/admin/news/4102/preview");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Preview provenance failure", body);
        Assert.DoesNotContain("Discovery provenance", body);
    }

    [Fact]
    public async Task GetDeleteUrl_redirectsToAdminList()
    {
        var client = CreateClient(AdminEmail, new SharedNewsStore());

        var response = await client.GetAsync("/admin/news/3001/delete");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/admin/news", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task AdminNewsList_is_paginated()
    {
        var store = new SharedNewsStore(CreateSeedArticles(55));
        var client = CreateClient(AdminEmail, store);

        var firstPage = await client.GetStringAsync("/admin/news");
        Assert.Contains("Showing 1&ndash;50 of 55 articles", firstPage);
        Assert.Contains("Article 055", firstPage);
        Assert.Contains("Article 006", firstPage);
        Assert.DoesNotContain("Article 005", firstPage);
        Assert.Contains("href=\"/admin/news/page/2\"", firstPage);

        var secondPage = await client.GetStringAsync("/admin/news/page/2");
        Assert.Contains("Showing 51&ndash;55 of 55 articles", secondPage);
        Assert.Contains("Article 005", secondPage);
        Assert.Contains("Article 001", secondPage);
        Assert.DoesNotContain("Article 006", secondPage);
        Assert.Contains("href=\"/admin/news\"", secondPage);

        var invalidPage = await client.GetAsync("/admin/news/page/99");
        Assert.Equal(HttpStatusCode.Redirect, invalidPage.StatusCode);
        Assert.Equal("/admin/news/page/2", invalidPage.Headers.Location!.OriginalString);

        var zeroPage = await client.GetAsync("/admin/news/page/0");
        Assert.Equal(HttpStatusCode.Redirect, zeroPage.StatusCode);
        Assert.Equal("/admin/news", zeroPage.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Delete_missingArticle_redirectsWithMessage()
    {
        var store = new SharedNewsStore(
        [
            new AdminNewsArticle(
                3002,
                "Still here",
                "still-here",
                "Excerpt",
                "Body",
                new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                null,
                false,
                DateTime.UtcNow,
                DateTime.UtcNow,
                AdminEmail)
        ]);
        var client = CreateClient(AdminEmail, store);

        var deleteResponse = await PostActionAsync(client, "/admin/news/6999/delete");
        Assert.Equal(HttpStatusCode.Redirect, deleteResponse.StatusCode);

        var listBody = await client.GetStringAsync(deleteResponse.Headers.Location!.OriginalString);
        Assert.Contains("News article 6999 was not found.", listBody);
        Assert.Contains("admin-status--error", listBody);
        Assert.Contains("Still here", listBody);
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
    public async Task EditDraft_can_save_when_publish_action_is_on_page()
    {
        var store = new SharedNewsStore(
        [
            new AdminNewsArticle(
                4205,
                "Draft to edit",
                "draft-to-edit",
                "Draft excerpt",
                "Draft body",
                new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                null,
                false,
                DateTime.UtcNow,
                DateTime.UtcNow,
                AdminEmail)
        ]);
        var client = CreateClient(AdminEmail, store);

        var saveResponse = await PostArticleAsync(
            client,
            "/admin/news/4205/edit",
            "/admin/news/4205",
            new Dictionary<string, string>
            {
                ["title"] = "Draft to edit updated",
                ["excerpt"] = "Updated excerpt",
                ["body"] = "Updated body",
                ["publishedAt"] = "2026-06-15"
            });

        Assert.Equal(HttpStatusCode.Redirect, saveResponse.StatusCode);
        Assert.Equal("/admin/news/4205/edit", saveResponse.Headers.Location!.OriginalString);

        var editBody = await client.GetStringAsync("/admin/news/4205/edit");
        Assert.Contains("Draft to edit updated", editBody);
        Assert.Contains("Updated body", editBody);
        Assert.Contains("Publish", editBody);
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

    private WebApplicationFactory<Program> CreateFactory(
        SharedNewsStore store,
        Action<IServiceCollection>? configureServices = null,
        INewsDiscoveryRepository? discoveryRepository = null) =>
        factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<SharedNewsStore>();
                services.RemoveAll<INewsRepository>();
                services.RemoveAll<IAdminNewsRepository>();
                services.RemoveAll<INewsAuditRepository>();
                services.RemoveAll<INewsDiscoveryRepository>();
                services.RemoveAll<SharedNewsDiscoveryStore>();
                services.AddSingleton(store);
                services.AddSingleton<INewsRepository>(_ => new QueenZone.Data.InMemoryNewsRepository(store));
                services.AddSingleton<IAdminNewsRepository>(_ => new InMemoryAdminNewsRepository(store));
                services.AddSingleton<INewsAuditRepository>(_ => new InMemoryNewsAuditRepository(store));
                if (discoveryRepository is not null)
                {
                    services.AddSingleton(discoveryRepository);
                }
                else
                {
                    services.AddSingleton<SharedNewsDiscoveryStore>();
                    services.AddSingleton<INewsDiscoveryRepository, InMemoryNewsDiscoveryRepository>();
                }

                configureServices?.Invoke(services);
            }));

    private HttpClient CreateClient(
        string? email = null,
        SharedNewsStore? store = null,
        Action<IServiceCollection>? configureServices = null,
        INewsDiscoveryRepository? discoveryRepository = null)
    {
        var appFactory = store is null ? factory : CreateFactory(store, configureServices, discoveryRepository);
        return CreateClientFromFactory(appFactory, email);
    }

    private HttpClient CreateClient(string? email, SharedNewsStore store) =>
        CreateClient(email, store, null, null);

    private WebApplicationFactory<Program> CreateFactory(SharedNewsStore store) =>
        CreateFactory(store, null, null);

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

    private static IEnumerable<AdminNewsArticle> CreateSeedArticles(int count) =>
        Enumerable.Range(1, count).Select(index => new AdminNewsArticle(
            index,
            $"Article {index:D3}",
            $"article-{index}",
            "Excerpt",
            "Body",
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(index),
            null,
            false,
            null,
            null,
            null));
}
