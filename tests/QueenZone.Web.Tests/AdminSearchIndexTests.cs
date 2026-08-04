using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace QueenZone.Web.Tests;

public sealed class AdminSearchIndexTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public AdminSearchIndexTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
    }

    [Fact]
    public async Task AdminSearchIndexPage_RendersDocumentCounts()
    {
        var client = AdminHttpTestHelpers.CreateClient(factory, AdminHttpTestHelpers.AdminEmail);

        var body = await client.GetStringAsync("/admin/search");

        Assert.Contains("Search index", body);
        Assert.Contains("documents indexed", body);
    }

    [Fact]
    public async Task AdminSearchIndexPage_RebuildsIndexOnPost()
    {
        var client = AdminHttpTestHelpers.CreateClient(factory, AdminHttpTestHelpers.AdminEmail);

        var response = await AdminHttpTestHelpers.PostArticleAsync(
            client,
            "/admin/search",
            "/admin/search?handler=Reindex",
            []);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var body = await client.GetStringAsync("/admin/search");
        Assert.Contains("Search index rebuilt.", body);
    }

    [Fact]
    public async Task AdminSearchIndexPage_RequiresAdminAuthentication()
    {
        var client = AdminHttpTestHelpers.CreateClient(factory);

        var response = await client.GetAsync("/admin/search");

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }
}
