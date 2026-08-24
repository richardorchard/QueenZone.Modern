using System.Net;
using System.Net.Http.Json;

namespace QueenZone.Web.Tests;

public sealed class ForumApiRecentThreadsTests : IClassFixture<QueenZoneWebApplicationFactory>
{
    private readonly QueenZoneWebApplicationFactory factory;

    public ForumApiRecentThreadsTests(QueenZoneWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task RecentThreads_requires_no_auth_and_returns_at_most_the_requested_count()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ForumApiEndpoints.RootPath}/recent-threads?count=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<IReadOnlyList<ForumRecentThreadDto>>();
        Assert.NotNull(payload);
        Assert.True(payload!.Count <= 2);
        Assert.All(payload, item => Assert.False(string.IsNullOrWhiteSpace(item.DetailPath)));
    }

    [Fact]
    public async Task RecentThreads_defaults_to_a_small_count_and_orders_most_recent_first()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ForumApiEndpoints.RootPath}/recent-threads");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<IReadOnlyList<ForumRecentThreadDto>>();
        Assert.NotNull(payload);
        Assert.Equal(3, payload!.Count);
        for (var i = 1; i < payload.Count; i++)
        {
            Assert.True(payload[i - 1].LastActivityAt >= payload[i].LastActivityAt);
        }
    }
}
