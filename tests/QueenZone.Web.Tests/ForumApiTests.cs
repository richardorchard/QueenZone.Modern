using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class ForumApiTests : IClassFixture<QueenZoneWebApplicationFactory>
{
    private readonly QueenZoneWebApplicationFactory factory;

    public ForumApiTests(QueenZoneWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Categories_list_requires_no_auth_and_matches_website_boards()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ForumApiEndpoints.RootPath}/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiPagedResponse<ForumCategoryListItemDto>>();
        Assert.NotNull(payload);
        Assert.Equal(1, payload!.Page);
        Assert.Equal(ApiPagination.DefaultPageSize, payload.PageSize);
        Assert.Equal(6, payload.TotalCount);
        Assert.Equal(6, payload.Items.Count);
        Assert.Equal("The Music", payload.Items[0].Name);
        Assert.Equal("/forum/1/the-music", payload.Items[0].DetailPath);
        Assert.Equal("The Lounge", payload.Items[^1].Name);
        Assert.All(payload.Items, item => Assert.False(string.IsNullOrWhiteSpace(item.DetailPath)));

        var html = await client.GetStringAsync("/forum");
        Assert.All(payload.Items, item =>
        {
            // Razor HTML-encodes board names (`Live & Tours` → `Live &amp; Tours`).
            Assert.Contains(WebUtility.HtmlEncode(item.Name), html, StringComparison.Ordinal);
            Assert.Contains(item.DetailPath, html, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task Categories_list_clamps_invalid_paging_query_values()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ForumApiEndpoints.RootPath}/categories?page=0&pageSize=1000");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiPagedResponse<ForumCategoryListItemDto>>();
        Assert.NotNull(payload);
        Assert.Equal(1, payload!.Page);
        Assert.Equal(ApiPagination.MaxPageSize, payload.PageSize);
        Assert.Equal(6, payload.Items.Count);
    }

    [Fact]
    public async Task Category_detail_returns_public_board()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ForumApiEndpoints.RootPath}/categories/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var item = await response.Content.ReadFromJsonAsync<ForumCategoryListItemDto>();
        Assert.NotNull(item);
        Assert.Equal(1, item!.Id);
        Assert.Equal("The Music", item.Name);
        Assert.Equal("/forum/1/the-music", item.DetailPath);
        Assert.Equal("Ranking every studio album", item.LatestThreadTitle);
    }

    [Fact]
    public async Task Category_detail_returns_problem_details_for_missing_board()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ForumApiEndpoints.RootPath}/categories/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(StatusCodes.Status404NotFound, problem.GetProperty("status").GetInt32());
        Assert.Contains("9999", problem.GetProperty("detail").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Topics_list_matches_website_category_page_order_and_paging()
    {
        using var client = factory.CreateAnonymousClient();

        using var firstPageResponse = await client.GetAsync(
            $"{ForumApiEndpoints.RootPath}/categories/1/topics?page=1&pageSize={ForumRoutes.TopicsPageSize}");
        Assert.Equal(HttpStatusCode.OK, firstPageResponse.StatusCode);
        var firstPage = await firstPageResponse.Content.ReadFromJsonAsync<ApiPagedResponse<ForumTopicListItemDto>>();
        Assert.NotNull(firstPage);
        Assert.Equal(1, firstPage!.Page);
        Assert.Equal(ForumRoutes.TopicsPageSize, firstPage.PageSize);
        Assert.Equal(30, firstPage.TotalCount);
        Assert.Equal(2, firstPage.TotalPages);
        Assert.Equal(ForumRoutes.TopicsPageSize, firstPage.Items.Count);
        Assert.Equal("Forum Guidelines", firstPage.Items[0].Title);
        Assert.True(firstPage.Items[0].IsSticky);
        Assert.Equal("/forum/topic/1001/forum-guidelines", firstPage.Items[0].DetailPath);
        Assert.Equal("Ranking every studio album", firstPage.Items[1].Title);
        Assert.False(firstPage.Items[1].IsSticky);

        var html = await client.GetStringAsync("/forum/1/the-music");
        Assert.Contains("Forum Guidelines", html, StringComparison.Ordinal);
        Assert.Contains(firstPage.Items[0].DetailPath, html, StringComparison.Ordinal);
        Assert.Contains(firstPage.Items[1].DetailPath, html, StringComparison.Ordinal);
        Assert.DoesNotContain("Archive sample thread 1030", html, StringComparison.Ordinal);

        using var secondPageResponse = await client.GetAsync(
            $"{ForumApiEndpoints.RootPath}/categories/1/topics?page=2&pageSize={ForumRoutes.TopicsPageSize}");
        var secondPage = await secondPageResponse.Content.ReadFromJsonAsync<ApiPagedResponse<ForumTopicListItemDto>>();
        Assert.NotNull(secondPage);
        Assert.Equal(2, secondPage!.Page);
        Assert.Contains(secondPage.Items, item => item.Title == "Archive sample thread 1030");

        var pageTwoHtml = await client.GetStringAsync("/forum/1/the-music/page/2");
        Assert.Contains("Archive sample thread 1030", pageTwoHtml, StringComparison.Ordinal);
        Assert.All(secondPage.Items, item => Assert.Contains(item.DetailPath, pageTwoHtml, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Topics_list_clamps_invalid_paging_query_values()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.GetAsync(
            $"{ForumApiEndpoints.RootPath}/categories/1/topics?page=0&pageSize=1000");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiPagedResponse<ForumTopicListItemDto>>();
        Assert.NotNull(payload);
        Assert.Equal(1, payload!.Page);
        Assert.Equal(ApiPagination.MaxPageSize, payload.PageSize);
        Assert.Equal(30, payload.Items.Count);
    }

    [Fact]
    public async Task Topics_list_returns_empty_page_for_board_with_no_topics()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ForumApiEndpoints.RootPath}/categories/2/topics");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiPagedResponse<ForumTopicListItemDto>>();
        Assert.NotNull(payload);
        Assert.Empty(payload!.Items);
        Assert.Equal(0, payload.TotalCount);
        Assert.Equal(0, payload.TotalPages);
    }

    [Fact]
    public async Task Topics_list_returns_problem_details_for_missing_board()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ForumApiEndpoints.RootPath}/categories/9999/topics");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public void Mapper_builds_same_canonical_paths_as_website()
    {
        var category = new ForumCategoryItem(
            1,
            "Queen - Serious Discussion",
            "Board",
            10,
            new DateTime(2024, 6, 12, 0, 0, 0, DateTimeKind.Utc),
            "Latest",
            10);
        var topic = new ForumTopicItem(
            1002,
            "Ranking every studio album",
            new DateTime(2024, 6, 12, 14, 0, 0, DateTimeKind.Utc),
            "brightonrock",
            1284,
            "brightonrock",
            false);

        var categoryDto = ForumApiMapper.ToCategoryListItem(category);
        var topicDto = ForumApiMapper.ToTopicListItem(topic);

        Assert.Equal("/forum/1/queen-serious-discussion", categoryDto.DetailPath);
        Assert.Equal("/forum/topic/1002/ranking-every-studio-album", topicDto.DetailPath);
        Assert.Equal(ForumApiMapper.ToCategoryListItems([category])[0], categoryDto);
        Assert.Equal(ForumApiMapper.ToTopicListItems([topic])[0], topicDto);
    }
}
