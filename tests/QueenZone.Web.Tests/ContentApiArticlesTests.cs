using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class ContentApiArticlesTests : IClassFixture<QueenZoneWebApplicationFactory>
{
    private readonly QueenZoneWebApplicationFactory factory;

    public ContentApiArticlesTests(QueenZoneWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Articles_list_requires_no_auth_and_returns_paged_archive_articles()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ContentApiEndpoints.RootPath}/articles?page=1&pageSize=5");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiPagedResponse<ArticleListItemDto>>();
        Assert.NotNull(payload);
        Assert.Equal(1, payload!.Page);
        Assert.Equal(5, payload.PageSize);
        Assert.Equal(5, payload.Items.Count);
        Assert.True(payload.TotalCount >= payload.Items.Count);
        Assert.Equal(101, payload.Items[0].Id);
        Assert.Equal("Inside the Making of Bohemian Rhapsody", payload.Items[0].Title);
        Assert.Equal("Recording", payload.Items[0].CategoryName);
        Assert.StartsWith("/articles/101/", payload.Items[0].DetailPath, StringComparison.Ordinal);
        Assert.All(payload.Items, item => Assert.False(string.IsNullOrWhiteSpace(item.DetailPath)));
        Assert.All(payload.Items, item => Assert.StartsWith("/articles/", item.DetailPath, StringComparison.Ordinal));
        Assert.DoesNotContain(payload.Items, item => item.Id == 9001);
        Assert.DoesNotContain(payload.Items, item => item.Title.Contains("modernisation", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Articles_list_defaults_to_archive_page_size_and_pages_server_side()
    {
        using var client = factory.CreateAnonymousClient();

        using var first = await client.GetAsync($"{ContentApiEndpoints.RootPath}/articles");
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var firstPage = await first.Content.ReadFromJsonAsync<ApiPagedResponse<ArticleListItemDto>>();
        Assert.NotNull(firstPage);
        Assert.Equal(1, firstPage!.Page);
        Assert.Equal(ArticlesRoutes.ArchivePageSize, firstPage.PageSize);
        Assert.Equal(ArticlesRoutes.ArchivePageSize, firstPage.Items.Count);
        Assert.True(firstPage.TotalCount > ArticlesRoutes.ArchivePageSize);
        Assert.Equal(2, firstPage.TotalPages);
        Assert.Equal(101, firstPage.Items[0].Id);
        Assert.DoesNotContain(firstPage.Items, item => item.Id == 122);

        using var second = await client.GetAsync($"{ContentApiEndpoints.RootPath}/articles?page=2");
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var secondPage = await second.Content.ReadFromJsonAsync<ApiPagedResponse<ArticleListItemDto>>();
        Assert.NotNull(secondPage);
        Assert.Equal(2, secondPage!.Page);
        Assert.Equal(ArticlesRoutes.ArchivePageSize, secondPage.PageSize);
        Assert.Equal(firstPage.TotalCount, secondPage.TotalCount);
        Assert.Equal(firstPage.TotalCount - ArticlesRoutes.ArchivePageSize, secondPage.Items.Count);
        Assert.Contains(secondPage.Items, item => item.Id == 122);
        Assert.DoesNotContain(secondPage.Items, item => item.Id == 101);
        Assert.DoesNotContain(secondPage.Items, item => item.Id == 9001);
    }

    [Fact]
    public async Task Articles_list_clamps_invalid_paging_query_values()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ContentApiEndpoints.RootPath}/articles?page=0&pageSize=1000");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiPagedResponse<ArticleListItemDto>>();
        Assert.NotNull(payload);
        Assert.Equal(1, payload!.Page);
        Assert.Equal(ApiPagination.MaxPageSize, payload.PageSize);
    }

    [Fact]
    public async Task Articles_detail_returns_published_article_body()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ContentApiEndpoints.RootPath}/articles/101");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var item = await response.Content.ReadFromJsonAsync<ArticleDetailDto>();
        Assert.NotNull(item);
        Assert.Equal(101, item!.Id);
        Assert.Equal("Inside the Making of Bohemian Rhapsody", item.Title);
        Assert.Equal("Recording", item.CategoryName);
        Assert.Equal("Queenzone archive", item.Source);
        Assert.False(string.IsNullOrWhiteSpace(item.Body));
        Assert.Contains("three studios", item.Body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<script", item.Body, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith("/articles/101/", item.DetailPath, StringComparison.Ordinal);
    }

    [Fact]
    public void ToArticleDetail_sanitizes_body_like_website_FormatBody()
    {
        var item = new ArticleItem(
            42,
            "Title",
            "Excerpt",
            "<script>alert(1)</script><p>Hello <em>world</em></p><iframe src=\"https://evil.example\"></iframe>",
            new DateTime(2026, 6, 11, 9, 0, 0, DateTimeKind.Utc),
            "https://www.queenzone.org/articles/42/title",
            "Features",
            true);

        var dto = ContentApiMapper.ToArticleDetail(item);

        Assert.DoesNotContain("script", dto.Body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("iframe", dto.Body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<em>world</em>", dto.Body);
        Assert.Equal("https://www.queenzone.org/articles/42/title", dto.Source);
        Assert.Equal("Features", dto.CategoryName);
        Assert.Equal(ArticlesRoutes.GetArticleDetailPath(item), dto.DetailPath);
        Assert.Equal(ArticleContent.FormatBody(item.Body), dto.Body);
    }

    [Fact]
    public void ToArticleListItem_maps_archive_fields_without_news_shape()
    {
        var item = new ArticleItem(
            42,
            "Title",
            "Excerpt",
            "Body",
            new DateTime(2026, 6, 11, 9, 0, 0, DateTimeKind.Utc),
            "Queenzone archive",
            "Recording",
            true);

        var dto = ContentApiMapper.ToArticleListItem(item);

        Assert.Equal(42, dto.Id);
        Assert.Equal("Title", dto.Title);
        Assert.Equal("Excerpt", dto.Excerpt);
        Assert.Equal(item.PublishedAt, dto.PublishedAt);
        Assert.Equal("Recording", dto.CategoryName);
        Assert.Equal(ArticlesRoutes.GetArticleDetailPath(item), dto.DetailPath);
    }

    [Fact]
    public async Task Articles_detail_returns_problem_details_for_unpublished_or_missing_article()
    {
        using var client = factory.CreateAnonymousClient();

        using var hidden = await client.GetAsync($"{ContentApiEndpoints.RootPath}/articles/9001");
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
        Assert.Equal("application/problem+json", hidden.Content.Headers.ContentType?.MediaType);
        var hiddenProblem = await hidden.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(StatusCodes.Status404NotFound, hiddenProblem.GetProperty("status").GetInt32());

        using var missing = await client.GetAsync($"{ContentApiEndpoints.RootPath}/articles/424242");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }
}
