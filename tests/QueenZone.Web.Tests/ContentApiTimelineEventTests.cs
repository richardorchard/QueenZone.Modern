using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QueenZone.Data;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class ContentApiTimelineEventTests : IClassFixture<QueenZoneWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly QueenZoneWebApplicationFactory factory;

    public ContentApiTimelineEventTests(QueenZoneWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Timeline_list_requires_no_auth()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ContentApiEndpoints.RootPath}/timeline");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Timeline_event_detail_returns_a_published_event_that_is_off_the_first_page()
    {
        var firstPage = Event(1, "First page event", new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var deep = Event(9999, "Deep off-page event", new DateTime(1985, 7, 13, 0, 0, 0, DateTimeKind.Utc));
        using var isolated = IsolatedEvents(firstPage, deep);
        using var client = isolated.CreateAnonymousClient();

        using var page = await client.GetAsync($"{ContentApiEndpoints.RootPath}/timeline?page=1&pageSize=1");
        var list = await ReadJsonAsync<ApiPagedResponse<TimelineEventDto>>(page);
        Assert.NotNull(list);
        Assert.Equal(2, list.TotalCount);
        Assert.DoesNotContain(list.Items, item => item.Id == 9999);

        using var response = await client.GetAsync($"{ContentApiEndpoints.RootPath}/timeline/9999");

        var payload = await ReadJsonAsync<TimelineEventDto>(response);
        Assert.NotNull(payload);
        Assert.Equal(9999, payload.Id);
        Assert.Equal("Deep off-page event", payload.Title);
        Assert.Equal("Queen play Live Aid.", payload.Summary);
        Assert.Equal("13 Jul 1985", payload.FormattedDate);
        Assert.False(string.IsNullOrWhiteSpace(payload.Category));
        Assert.False(string.IsNullOrWhiteSpace(payload.CategoryLabel));
        Assert.Equal("https://en.wikipedia.org/wiki/Live_Aid", payload.SourceUrl);
    }

    [Fact]
    public async Task Timeline_event_detail_returns_404_for_unpublished_or_missing()
    {
        using var isolated = IsolatedEvents(
            Event(13, "Draft event", new DateTime(1975, 10, 31, 0, 0, 0, DateTimeKind.Utc), isPublished: false));
        using var client = isolated.CreateAnonymousClient();

        using var unpublished = await client.GetAsync($"{ContentApiEndpoints.RootPath}/timeline/13");
        Assert.Equal(HttpStatusCode.NotFound, unpublished.StatusCode);
        Assert.Equal("application/problem+json", unpublished.Content.Headers.ContentType?.MediaType);

        using var missing = await client.GetAsync($"{ContentApiEndpoints.RootPath}/timeline/424242");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    private static QueenZoneWebApplicationFactory IsolatedEvents(params QueenHistoryEvent[] events) =>
        QueenZoneWebApplicationFactory.WithServices(services =>
        {
            services.RemoveAll<IQueenHistoryRepository>();
            services.AddSingleton<IQueenHistoryRepository>(new InMemoryQueenHistoryRepository(events));
        });

    private static QueenHistoryEvent Event(
        int id,
        string title,
        DateTime eventDate,
        bool isPublished = true) =>
        new(
            id,
            title,
            "Queen play Live Aid.",
            eventDate,
            QueenHistoryDatePrecision.ExactDate,
            QueenHistoryEventCategory.Concert,
            100,
            QueenHistoryEventSourceType.Wikipedia,
            $"event-{id}",
            "https://en.wikipedia.org/wiki/Live_Aid",
            isPublished);

    private static async Task<T?> ReadJsonAsync<T>(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.False(string.IsNullOrWhiteSpace(body), "Timeline JSON must not be an empty 200 body.");
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        return JsonSerializer.Deserialize<T>(body, JsonOptions);
    }
}
