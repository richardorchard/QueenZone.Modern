using System.Net;
using System.Net.Http.Json;

namespace QueenZone.Web.Tests;

public sealed class ContentApiBiographyTests : IClassFixture<QueenZoneWebApplicationFactory>
{
    private readonly QueenZoneWebApplicationFactory factory;

    public ContentApiBiographyTests(QueenZoneWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Biography_list_requires_no_auth_and_returns_chapters_in_reading_order()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ContentApiEndpoints.RootPath}/biography");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiPagedResponse<BiographyChapterListItemDto>>();
        Assert.NotNull(payload);
        Assert.True(payload!.Items.Count >= 5);
        Assert.Equal(1, payload.Items[0].DisplaySequence);
        Assert.True(payload.Items[0].DisplaySequence < payload.Items[1].DisplaySequence);
        Assert.All(payload.Items, item => Assert.False(string.IsNullOrWhiteSpace(item.DetailPath)));
    }

    [Fact]
    public async Task Biography_list_clamps_invalid_paging_query_values()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ContentApiEndpoints.RootPath}/biography?page=0&pageSize=1000");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiPagedResponse<BiographyChapterListItemDto>>();
        Assert.NotNull(payload);
        Assert.Equal(1, payload!.Page);
        Assert.Equal(ApiPagination.MaxPageSize, payload.PageSize);
    }

    [Fact]
    public async Task Biography_detail_returns_chapter_body_and_adjacent_navigation()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ContentApiEndpoints.RootPath}/biography/2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var chapter = await response.Content.ReadFromJsonAsync<BiographyChapterDetailDto>();
        Assert.NotNull(chapter);
        Assert.Equal(2, chapter!.Id);
        Assert.False(string.IsNullOrWhiteSpace(chapter.Body));
        Assert.NotNull(chapter.Previous);
        Assert.Equal(1, chapter.Previous!.Id);
        Assert.NotNull(chapter.Next);
        Assert.Equal(3, chapter.Next!.Id);
    }

    [Fact]
    public async Task Biography_detail_returns_problem_details_for_missing_chapter()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ContentApiEndpoints.RootPath}/biography/424242");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
