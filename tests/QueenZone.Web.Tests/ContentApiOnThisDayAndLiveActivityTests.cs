using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class ContentApiOnThisDayAndLiveActivityTests : IClassFixture<QueenZoneWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly QueenZoneWebApplicationFactory factory;

    public ContentApiOnThisDayAndLiveActivityTests(QueenZoneWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task OnThisDay_requires_no_auth_and_returns_200()
    {
        // Pin the clock: sample seed is sparse. 27 Aug 2026 (the CI flake date) has
        // no exact match and nothing inside the +/-7 day window (John Deacon is
        // 19 Aug; Freddie's birthday is 5 Sep).
        using var isolated = CreateFactoryForUtcDate(2026, 7, 13);
        using var client = isolated.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ContentApiEndpoints.RootPath}/on-this-day");

        var payload = await ReadOnThisDayJsonAsync<TimelineEventDto?>(response);
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload.Title));
        Assert.False(string.IsNullOrWhiteSpace(payload.FormattedDate));
        Assert.Equal("Queen's Live Aid performance", payload.Title);
    }

    [Fact]
    public async Task OnThisDay_falls_back_to_nearby_seed_event()
    {
        using var isolated = CreateFactoryForUtcDate(2026, 7, 12);
        using var client = isolated.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ContentApiEndpoints.RootPath}/on-this-day");

        var payload = await ReadOnThisDayJsonAsync<TimelineEventDto?>(response);
        Assert.NotNull(payload);
        Assert.Equal("Queen's Live Aid performance", payload.Title);
    }

    [Fact]
    public async Task OnThisDay_returns_json_null_when_seed_has_no_nearby_event()
    {
        using var isolated = CreateFactoryForUtcDate(2026, 8, 27);
        using var client = isolated.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ContentApiEndpoints.RootPath}/on-this-day");

        var payload = await ReadOnThisDayJsonAsync<TimelineEventDto?>(response);
        Assert.Null(payload);
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

    private static QueenZoneWebApplicationFactory CreateFactoryForUtcDate(int year, int month, int day) =>
        QueenZoneWebApplicationFactory.WithServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(
                new FixedTimeProvider(new DateTimeOffset(year, month, day, 12, 0, 0, TimeSpan.Zero)));
        });

    private static async Task<T?> ReadOnThisDayJsonAsync<T>(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadAsStringAsync();
        Assert.False(
            string.IsNullOrWhiteSpace(body),
            "OnThisDay must return JSON (object or null), not an empty 200 body.");

        return JsonSerializer.Deserialize<T>(body, JsonOptions);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
