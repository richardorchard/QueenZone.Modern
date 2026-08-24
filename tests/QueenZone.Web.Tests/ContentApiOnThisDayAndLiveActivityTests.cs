using System.Net;
using System.Net.Http.Json;

namespace QueenZone.Web.Tests;

public sealed class ContentApiOnThisDayAndLiveActivityTests : IClassFixture<QueenZoneWebApplicationFactory>
{
    private readonly QueenZoneWebApplicationFactory factory;

    public ContentApiOnThisDayAndLiveActivityTests(QueenZoneWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task OnThisDay_requires_no_auth_and_returns_200()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ContentApiEndpoints.RootPath}/on-this-day");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // Seed data spans many dates; the +/-7 day fallback means an event is expected on
        // essentially any date, but absence (null body) is also a valid, non-error response.
        var payload = await response.Content.ReadFromJsonAsync<TimelineEventDto?>();
        if (payload is not null)
        {
            Assert.False(string.IsNullOrWhiteSpace(payload.Title));
            Assert.False(string.IsNullOrWhiteSpace(payload.FormattedDate));
        }
    }

    [Fact]
    public async Task LiveActivity_requires_no_auth_and_returns_a_non_negative_count()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ContentApiEndpoints.RootPath}/live-activity");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<LiveActivitySummaryDto>();
        Assert.NotNull(payload);
        Assert.True(payload!.NewForumRepliesToday >= 0);
    }
}
