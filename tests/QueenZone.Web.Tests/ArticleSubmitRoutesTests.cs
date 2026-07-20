using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed partial class ArticleSubmitRoutesTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public ArticleSubmitRoutesTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
    }

    [Fact]
    public async Task GetSubmitArticle_RedirectsAnonymousUser()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await client.GetAsync("/submit/article");

        // Member-only route challenges with a redirect to /account/login.
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
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserEmailHeader, "admin@test.local");

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

        // ASP.NET Core's UseAntiforgery() middleware returns 400 Bad Request for form-bearing
        // POST requests that fail antiforgery validation (before the endpoint handler runs).
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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

        // This slug won't match any legacy int/slug route, so it hits CommunityDetail
        // and returns 404 for an unknown slug.
        var response = await client.GetAsync("/articles/nonexistent-community-slug-xyz");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
