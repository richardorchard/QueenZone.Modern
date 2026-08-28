using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QueenZone.Data;
using QueenZone.Storage;

namespace QueenZone.Web.Tests;

public sealed class FanPerformancesPageTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public FanPerformancesPageTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                services
                    .AddAuthentication()
                    .AddScheme<AuthenticationSchemeOptions, ExternalCookieTestHandler>(
                        MemberAuthenticationSchemes.ExternalCookie, _ => { });
            });
        });
    }

    [Fact]
    public async Task FanPerformancesPageRendersSeedPerformancesForAnonymousVisitors()
    {
        var client = factory.CreateClient();

        var body = await client.GetStringAsync("/fan-performances");

        Assert.Contains("Fan Performances", body);
        Assert.Contains("Reaching Out", body);
        Assert.Contains("Mike Ryde", body);
        Assert.Contains("Sign in to play", body);
        Assert.Contains("aria-label=\"Sign in to play Reaching Out\"", body);
        Assert.Contains("class=\"qz-stage-play\"", body);
        Assert.DoesNotContain("data-qz-stage-play", body);
        Assert.DoesNotContain("data-qz-stage-play-all", body);
        Assert.DoesNotContain("data-qz-stage-shuffle-all", body);
        Assert.DoesNotContain("data-qz-stage-catalog", body);
        Assert.DoesNotContain("Play All", body);
        Assert.DoesNotContain("Shuffle Play All", body);
        Assert.Contains("returnUrl=%2Ffan-performances", body);
        Assert.DoesNotContain("songfiles", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/fan-performances/187/audio", body);
        Assert.Contains(TestSiteConfiguration.CanonicalLink("/fan-performances"), body);
    }

    [Fact]
    public async Task FanPerformancesPage_ShowsPlayControl_WhenMemberSignedIn()
    {
        var client = await CreateSignedInMemberClientAsync();

        var body = await client.GetStringAsync("/fan-performances");

        Assert.Contains("/fan-performances/187/audio", body);
        Assert.Contains("data-qz-stage-play", body);
        Assert.Contains("aria-label=\"Play Reaching Out\"", body);
        Assert.Contains("class=\"qz-stage-play\"", body);
        Assert.Contains("data-qz-stage-play-all", body);
        Assert.Contains("data-qz-stage-shuffle-all", body);
        Assert.Contains(">Play All</button>", body);
        Assert.Contains(">Shuffle Play All</button>", body);
        Assert.DoesNotContain("Sign in to play", body);
        Assert.DoesNotContain("songfiles", body, StringComparison.OrdinalIgnoreCase);

        var catalog = ReadCatalog(body);
        Assert.Equal([187, 186, 176, 173], catalog.Select(entry => entry.Id).ToArray());
        Assert.All(catalog, entry => Assert.StartsWith("/fan-performances/", entry.AudioPlayPath));
        Assert.All(catalog, entry => Assert.DoesNotContain("songfiles", entry.AudioPlayPath, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task FanPerformancesPage_ShowsPlayControl_WhenTestMemberHeaderIsPresent()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestMemberAuthHandler.MemberIdHeader, Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add(TestMemberAuthHandler.DisplayNameHeader, "Stage Member");

        var body = await client.GetStringAsync("/fan-performances");

        Assert.Contains("/fan-performances/187/audio", body);
        Assert.Contains("data-qz-stage-play", body);
        Assert.Contains("aria-label=\"Play Reaching Out\"", body);
        Assert.Contains("data-qz-stage-play-all", body);
        Assert.DoesNotContain("Sign in to play", body);
    }

    [Fact]
    public async Task FanPerformancesPage_PlayAllCatalogSpansEveryPage_WhenMemberSignedIn()
    {
        var performances = Enumerable.Range(1, 25)
            .Select(index => new FanPerformance(
                index,
                $"Track {index}",
                "Performer",
                "Cover",
                $"{index}.mp3",
                1024,
                new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(index)))
            .ToList();
        await using var customFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IFanPerformanceRepository>();
                services.AddSingleton<IFanPerformanceRepository>(new InMemoryFanPerformanceRepository(performances));
            });
        });
        var client = await CreateSignedInMemberClientAsync(factory: customFactory);

        var pageOne = await client.GetStringAsync("/fan-performances");
        var pageTwo = await client.GetStringAsync("/fan-performances/page/2");

        var pageOneCatalog = ReadCatalog(pageOne);
        var pageTwoCatalog = ReadCatalog(pageTwo);
        var expectedIds = Enumerable.Range(1, 25).Reverse().ToArray();

        Assert.Equal(expectedIds, pageOneCatalog.Select(entry => entry.Id).ToArray());
        Assert.Equal(expectedIds, pageTwoCatalog.Select(entry => entry.Id).ToArray());
        Assert.Contains("data-qz-stage-play-all", pageTwo);
        Assert.Contains("data-qz-stage-shuffle-all", pageTwo);
        Assert.Equal(20, CountOccurrences(pageOne, "aria-label=\"Play Track"));
        Assert.Equal(5, CountOccurrences(pageTwo, "aria-label=\"Play Track"));
        Assert.Equal(20, CountOccurrences(pageOne, "<audio "));
        Assert.Equal(5, CountOccurrences(pageTwo, "<audio "));
        Assert.Contains(">Track 25</h2>", pageOne);
        Assert.DoesNotContain(">Track 5</h2>", pageOne);
        Assert.Contains(">Track 5</h2>", pageTwo);
    }

    [Fact]
    public async Task FanPerformancesPageTwo_RedirectsToIndex_WhenOnlyOnePageOfSeedData()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/fan-performances/page/2");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AudioEndpoint_RedirectsAnonymousVisitorsToLogin()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/fan-performances/187/audio");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/account/login", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task AudioEndpoint_StreamsAudioForSignedInMembers()
    {
        await SeedSampleSongfileAsync();
        var client = await CreateSignedInMemberClientAsync(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/fan-performances/187/audio");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(response.Headers.Location);
        Assert.Equal("audio/mpeg", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("Reaching-Out.mp3", response.Content.Headers.ContentDisposition?.ToString());
        Assert.Equal("ID3fake-audio"u8.ToArray(), await response.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task AudioEndpoint_DoesNotExposePublicBlobLocation()
    {
        await SeedSampleSongfileAsync();
        var client = await CreateSignedInMemberClientAsync(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/fan-performances/187/audio");
        var location = response.Headers.Location?.OriginalString ?? string.Empty;
        var body = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("cdn2.queenzone.org", location, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("blob.core.windows.net", location, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("cdn2.queenzone.org", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AudioEndpoint_ReturnsNotFound_WhenPerformanceDoesNotExist()
    {
        var client = await CreateSignedInMemberClientAsync(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/fan-performances/999999/audio");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task SeedSampleSongfileAsync()
    {
        var backend = factory.Services.GetRequiredService<IBlobStorageBackend>();
        await using var audio = new MemoryStream(Encoding.ASCII.GetBytes("ID3fake-audio"));
        await backend.UploadAsync(
            SongFileUrl.ContainerName,
            "2014417798057369.mp3",
            audio,
            "audio/mpeg");
    }

    private async Task<HttpClient> CreateSignedInMemberClientAsync(
        WebApplicationFactoryClientOptions? options = null,
        WebApplicationFactory<Program>? factory = null)
    {
        var host = factory ?? this.factory;
        var client = host.CreateClient(options ?? new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false,
        });
        client.DefaultRequestHeaders.Add(ExternalCookieTestHandler.ProviderHeader, "Google");
        client.DefaultRequestHeaders.Add(ExternalCookieTestHandler.SubjectHeader, "google-fan-stage-subject");
        client.DefaultRequestHeaders.Add(ExternalCookieTestHandler.EmailHeader, "fanstage@example.com");
        client.DefaultRequestHeaders.Add(ExternalCookieTestHandler.NameHeader, "Fan Stage Member");

        var callbackResponse = await client.GetAsync("/account/external-login-callback");
        Assert.Equal(HttpStatusCode.Redirect, callbackResponse.StatusCode);
        Assert.DoesNotContain("/account/login", callbackResponse.Headers.Location!.OriginalString);

        return client;
    }

    private static FanPerformanceCatalogEntry[] ReadCatalog(string body)
    {
        const string marker = "<script type=\"application/json\" data-qz-stage-catalog>";
        var start = body.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(start >= 0, "Signed-in fan performance pages must embed the catalog payload.");
        start += marker.Length;
        var end = body.IndexOf("</script>", start, StringComparison.Ordinal);
        Assert.True(end > start);
        var json = body[start..end];
        var catalog = JsonSerializer.Deserialize<FanPerformanceCatalogEntry[]>(
            json,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        Assert.NotNull(catalog);
        return catalog;
    }

    private static int CountOccurrences(string body, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = body.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }
}
