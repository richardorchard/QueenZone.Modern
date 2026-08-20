using System.Net;
using System.Net.Http.Json;

namespace QueenZone.Web.Tests;

public sealed class ContentApiFreddieTributeTests : IClassFixture<QueenZoneWebApplicationFactory>
{
    private readonly QueenZoneWebApplicationFactory factory;

    public ContentApiFreddieTributeTests(QueenZoneWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task FreddieTribute_list_requires_no_auth_and_returns_paged_response()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ContentApiEndpoints.RootPath}/freddietribute?page=1&pageSize=5");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiPagedResponse<FreddieTributeDto>>();
        Assert.NotNull(payload);
        Assert.Equal(1, payload!.Page);
        Assert.Equal(5, payload.PageSize);
        Assert.Equal(5, payload.Items.Count);
        Assert.True(payload.TotalCount >= 14);
        Assert.All(payload.Items, item => Assert.False(string.IsNullOrWhiteSpace(item.Name)));
        Assert.All(payload.Items, item => Assert.False(string.IsNullOrWhiteSpace(item.Thought)));
    }

    [Fact]
    public async Task FreddieTribute_list_clamps_invalid_paging_query_values()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ContentApiEndpoints.RootPath}/freddietribute?page=0&pageSize=1000");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiPagedResponse<FreddieTributeDto>>();
        Assert.NotNull(payload);
        Assert.Equal(1, payload!.Page);
        Assert.Equal(ApiPagination.MaxPageSize, payload.PageSize);
    }

    [Fact]
    public async Task FreddieTribute_list_returns_empty_items_past_the_last_page()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ContentApiEndpoints.RootPath}/freddietribute?page=999&pageSize=5");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiPagedResponse<FreddieTributeDto>>();
        Assert.NotNull(payload);
        Assert.Empty(payload!.Items);
    }
}
