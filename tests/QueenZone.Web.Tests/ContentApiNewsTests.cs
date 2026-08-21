using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace QueenZone.Web.Tests;

public sealed class ContentApiNewsTests : IClassFixture<QueenZoneWebApplicationFactory>
{
    private readonly QueenZoneWebApplicationFactory factory;

    public ContentApiNewsTests(QueenZoneWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task News_list_requires_no_auth_and_returns_paged_response()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ContentApiEndpoints.RootPath}/news?page=1&pageSize=5");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiPagedResponse<NewsListItemDto>>();
        Assert.NotNull(payload);
        Assert.Equal(1, payload!.Page);
        Assert.Equal(5, payload.PageSize);
        Assert.Equal(5, payload.Items.Count);
        Assert.True(payload.TotalCount >= payload.Items.Count);
        Assert.All(payload.Items, item => Assert.False(string.IsNullOrWhiteSpace(item.DetailPath)));
    }

    [Fact]
    public async Task News_list_clamps_invalid_paging_query_values()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ContentApiEndpoints.RootPath}/news?page=0&pageSize=1000");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiPagedResponse<NewsListItemDto>>();
        Assert.NotNull(payload);
        Assert.Equal(1, payload!.Page);
        Assert.Equal(ApiPagination.MaxPageSize, payload.PageSize);
    }

    [Fact]
    public async Task News_detail_returns_published_article_body()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ContentApiEndpoints.RootPath}/news/1003");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var item = await response.Content.ReadFromJsonAsync<NewsDetailDto>();
        Assert.NotNull(item);
        Assert.Equal(1003, item!.Id);
        Assert.Equal("QueenZone modernisation begins", item.Title);
        Assert.False(string.IsNullOrWhiteSpace(item.Body));
        // Sanitized HTML parity with the website (#728): formatting, links, UGC images.
        Assert.Contains("<strong>ASP.NET Core</strong>", item.Body);
        Assert.Contains("href=\"https://www.queenzone.org/news\"", item.Body);
        Assert.Contains("src=\"/ugc/news/sample-crest.jpg\"", item.Body);
        Assert.DoesNotContain("<script", item.Body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ToNewsDetail_sanitizes_body_like_website_FormatBody()
    {
        var item = new QueenZone.Data.NewsItem(
            42,
            "Title",
            "Excerpt",
            "<script>alert(1)</script><p>Hello <em>world</em></p><iframe src=\"https://evil.example\"></iframe>",
            new DateTime(2026, 6, 11, 9, 0, 0, DateTimeKind.Utc),
            null,
            true);

        var dto = ContentApiMapper.ToNewsDetail(item);

        Assert.DoesNotContain("script", dto.Body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("iframe", dto.Body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<em>world</em>", dto.Body);
    }

    [Fact]
    public async Task News_detail_returns_problem_details_for_unpublished_or_missing_article()
    {
        using var client = factory.CreateAnonymousClient();

        using var hidden = await client.GetAsync($"{ContentApiEndpoints.RootPath}/news/9001");
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
        Assert.Equal("application/problem+json", hidden.Content.Headers.ContentType?.MediaType);
        var hiddenProblem = await hidden.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(StatusCodes.Status404NotFound, hiddenProblem.GetProperty("status").GetInt32());

        using var missing = await client.GetAsync($"{ContentApiEndpoints.RootPath}/news/424242");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }
}
