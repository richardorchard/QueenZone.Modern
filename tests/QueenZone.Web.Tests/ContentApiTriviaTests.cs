using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class ContentApiTriviaTests : IClassFixture<QueenZoneWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly QueenZoneWebApplicationFactory factory;

    public ContentApiTriviaTests(QueenZoneWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Random_trivia_requires_no_auth_and_returns_a_published_fact()
    {
        using var client = factory.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ContentApiEndpoints.RootPath}/trivia/random");

        var payload = await ReadRandomTriviaJsonAsync<TriviaDto?>(response);
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload.Text));
        Assert.True(payload.Id > 0);
    }

    [Fact]
    public async Task Random_trivia_returns_json_null_when_nothing_is_published()
    {
        using var isolated = QueenZoneWebApplicationFactory.WithServices(services =>
        {
            services.RemoveAll<ITriviaRepository>();
            services.AddSingleton<ITriviaRepository>(new InMemoryTriviaRepository([]));
        });
        using var client = isolated.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ContentApiEndpoints.RootPath}/trivia/random");

        var payload = await ReadRandomTriviaJsonAsync<TriviaDto?>(response);
        Assert.Null(payload);
    }

    [Fact]
    public async Task Random_trivia_returns_json_null_when_only_unpublished_facts_exist()
    {
        using var isolated = IsolatedTrivia(
            new TriviaFactItem(21, "Draft only", DateTime.UtcNow, false, "Band", TriviaDifficulty.Easy, "Notes"));
        using var client = isolated.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ContentApiEndpoints.RootPath}/trivia/random");

        var payload = await ReadRandomTriviaJsonAsync<TriviaDto?>(response);
        Assert.Null(payload);
    }

    [Fact]
    public async Task Random_trivia_returns_optional_fields()
    {
        using var isolated = IsolatedTrivia(
            new TriviaFactItem(
                22,
                "Freddie Mercury was born Farrokh Bulsara.",
                DateTime.UtcNow,
                true,
                "Band",
                TriviaDifficulty.Easy,
                "Queen official biography"));
        using var client = isolated.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ContentApiEndpoints.RootPath}/trivia/random");

        var payload = await ReadRandomTriviaJsonAsync<TriviaDto>(response);
        Assert.NotNull(payload);
        Assert.Equal(22, payload.Id);
        Assert.Equal("Freddie Mercury was born Farrokh Bulsara.", payload.Text);
        Assert.Equal("Band", payload.Category);
        Assert.Equal(TriviaDifficulty.Easy, payload.Difficulty);
        Assert.Equal("Queen official biography", payload.Source);
    }

    [Fact]
    public async Task Random_trivia_omits_blank_optional_fields()
    {
        using var isolated = IsolatedTrivia(
            new TriviaFactItem(23, "We Will Rock You uses stadium stomp percussion.", DateTime.UtcNow, true, "   ", "  ", "   "));
        using var client = isolated.CreateAnonymousClient();

        using var response = await client.GetAsync($"{ContentApiEndpoints.RootPath}/trivia/random");

        var payload = await ReadRandomTriviaJsonAsync<TriviaDto>(response);
        Assert.NotNull(payload);
        Assert.Equal(23, payload.Id);
        Assert.Null(payload.Category);
        Assert.Null(payload.Difficulty);
        Assert.Null(payload.Source);
    }

    [Fact]
    public void Mapper_omits_blank_optional_fields()
    {
        var dto = ContentApiMapper.ToTriviaDto(
            new TriviaFactItem(24, "A fact", DateTime.UtcNow, true, "   ", null, "  "));

        Assert.Equal(24, dto.Id);
        Assert.Equal("A fact", dto.Text);
        Assert.Null(dto.Category);
        Assert.Null(dto.Difficulty);
        Assert.Null(dto.Source);
    }

    private static QueenZoneWebApplicationFactory IsolatedTrivia(params TriviaFactItem[] facts) =>
        QueenZoneWebApplicationFactory.WithServices(services =>
        {
            services.RemoveAll<ITriviaRepository>();
            services.AddSingleton<ITriviaRepository>(new InMemoryTriviaRepository(facts));
        });

    private static async Task<T?> ReadRandomTriviaJsonAsync<T>(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.False(
            string.IsNullOrWhiteSpace(body),
            "Random trivia must return JSON (object or null), not an empty 200 body.");
        Assert.Equal(
            "application/json",
            response.Content.Headers.ContentType?.MediaType);

        return JsonSerializer.Deserialize<T>(body, JsonOptions);
    }
}
