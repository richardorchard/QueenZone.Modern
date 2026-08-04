using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace QueenZone.Web.Tests;

/// <summary>
/// Verifies admin publish/unpublish write paths immediately keep the unified search index in
/// sync, rather than relying solely on the next scheduled batch reindex.
/// </summary>
[Collection(AdminNewsDeleteErrorCollection.Name)]
public sealed class SearchIndexSyncTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public SearchIndexSyncTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
    }

    [Fact]
    public async Task PublishingNewsArticle_MakesItImmediatelySearchable()
    {
        var client = AdminHttpTestHelpers.CreateClient(factory, AdminHttpTestHelpers.AdminEmail);

        var createResponse = await AdminHttpTestHelpers.PostArticleAsync(
            client,
            "/admin/news/new",
            "/admin/news",
            new Dictionary<string, string>
            {
                ["title"] = "Search sync publish title",
                ["excerpt"] = "Created to exercise search index sync.",
                ["body"] = "Plain text body for search index sync.",
                ["publishedAt"] = "2026-06-14",
            });
        var articleId = AdminHttpTestHelpers.ParseNewsIdFromEditRedirect(createResponse);

        var beforePublish = await client.GetStringAsync("/search?q=search+sync+publish");
        Assert.DoesNotContain("Search sync publish title", beforePublish);

        var publishResponse = await AdminHttpTestHelpers.PostNewsActionAsync(client, $"/admin/news/{articleId}/publish");
        Assert.Equal(HttpStatusCode.Redirect, publishResponse.StatusCode);

        var afterPublish = await client.GetStringAsync("/search?q=search+sync+publish");
        Assert.Contains("Search sync publish title", afterPublish);
    }

    [Fact]
    public async Task UnpublishingNewsArticle_RemovesItFromSearchImmediately()
    {
        var client = AdminHttpTestHelpers.CreateClient(factory, AdminHttpTestHelpers.AdminEmail);

        var createResponse = await AdminHttpTestHelpers.PostArticleAsync(
            client,
            "/admin/news/new",
            "/admin/news",
            new Dictionary<string, string>
            {
                ["title"] = "Search sync unpublish title",
                ["excerpt"] = "Created to exercise search index sync.",
                ["body"] = "Plain text body for search index sync.",
                ["publishedAt"] = "2026-06-14",
            });
        var articleId = AdminHttpTestHelpers.ParseNewsIdFromEditRedirect(createResponse);

        await AdminHttpTestHelpers.PostNewsActionAsync(client, $"/admin/news/{articleId}/publish");
        var afterPublish = await client.GetStringAsync("/search?q=search+sync+unpublish");
        Assert.Contains("Search sync unpublish title", afterPublish);

        var unpublishResponse = await AdminHttpTestHelpers.PostNewsActionAsync(client, $"/admin/news/{articleId}/unpublish");
        Assert.Equal(HttpStatusCode.Redirect, unpublishResponse.StatusCode);

        var afterUnpublish = await client.GetStringAsync("/search?q=search+sync+unpublish");
        Assert.DoesNotContain("Search sync unpublish title", afterUnpublish);
    }
}
