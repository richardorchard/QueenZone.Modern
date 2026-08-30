using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class ContentApiQuoteTests : IClassFixture<QueenZoneWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly QueenZoneWebApplicationFactory factory;

    public ContentApiQuoteTests(QueenZoneWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Random_quote_requires_no_auth_and_returns_a_published_quote()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ContentApiEndpoints.RootPath}/quotes/random");

        var payload = await ReadRandomQuoteJsonAsync<QuoteDto?>(response);
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload.Text));
        Assert.False(string.IsNullOrWhiteSpace(payload.WhoSaid));
    }

    [Fact]
    public async Task Random_quote_returns_json_null_when_nothing_is_published()
    {
        using var isolated = QueenZoneWebApplicationFactory.WithServices(services =>
        {
            services.RemoveAll<IQuoteRepository>();
            services.AddSingleton<IQuoteRepository>(new InMemoryQuoteRepository([]));
        });
        using var client = isolated.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ContentApiEndpoints.RootPath}/quotes/random");

        var payload = await ReadRandomQuoteJsonAsync<QuoteDto?>(response);
        Assert.Null(payload);
    }

    [Fact]
    public async Task Quote_detail_returns_published_quote_with_context()
    {
        using var isolated = IsolatedQuotes(
            new QuoteItem(11, "A kind of magic", "Freddie Mercury", DateTime.UtcNow, true, "Live Aid, 1985"));
        using var client = isolated.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ContentApiEndpoints.RootPath}/quotes/11");

        var payload = await ReadRandomQuoteJsonAsync<QuoteDto>(response);
        Assert.NotNull(payload);
        Assert.Equal(11, payload.Id);
        Assert.Equal("A kind of magic", payload.Text);
        Assert.Equal("Freddie Mercury", payload.WhoSaid);
        Assert.Equal("Live Aid, 1985", payload.Context);
    }

    [Fact]
    public async Task Quote_detail_omits_blank_context()
    {
        using var isolated = IsolatedQuotes(
            new QuoteItem(12, "We will rock you", "Brian May", DateTime.UtcNow, true, "   "));
        using var client = isolated.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ContentApiEndpoints.RootPath}/quotes/12");

        var payload = await ReadRandomQuoteJsonAsync<QuoteDto>(response);
        Assert.NotNull(payload);
        Assert.Equal(12, payload.Id);
        Assert.Null(payload.Context);
    }

    [Fact]
    public async Task Quote_detail_returns_404_for_unpublished_or_missing()
    {
        using var isolated = IsolatedQuotes(
            new QuoteItem(13, "Draft line", "Roger Taylor", DateTime.UtcNow, false, "Studio notes"));
        using var client = isolated.CreateAnonymousClient();

        using var unpublished = await client.GetAsync($"{ContentApiEndpoints.RootPath}/quotes/13");
        Assert.Equal(HttpStatusCode.NotFound, unpublished.StatusCode);
        Assert.Equal("application/problem+json", unpublished.Content.Headers.ContentType?.MediaType);

        using var missing = await client.GetAsync($"{ContentApiEndpoints.RootPath}/quotes/424242");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    private static QueenZoneWebApplicationFactory IsolatedQuotes(params QuoteItem[] quotes) =>
        QueenZoneWebApplicationFactory.WithServices(services =>
        {
            services.RemoveAll<IQuoteRepository>();
            services.AddSingleton<IQuoteRepository>(new InMemoryQuoteRepository(quotes));
        });

    private static async Task<T?> ReadRandomQuoteJsonAsync<T>(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.False(
            string.IsNullOrWhiteSpace(body),
            "Random quote must return JSON (object or null), not an empty 200 body.");
        Assert.Equal(
            "application/json",
            response.Content.Headers.ContentType?.MediaType);

        return JsonSerializer.Deserialize<T>(body, JsonOptions);
    }
}
