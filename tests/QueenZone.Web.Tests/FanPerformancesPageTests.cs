using System.Net;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
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
        Assert.Contains("returnUrl=%2Ffan-performances", body);
        Assert.DoesNotContain("songfiles", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/fan-performances/187/audio", body);
        Assert.Contains(TestSiteConfiguration.CanonicalLink("/fan-performances"), body);
    }

    [Fact]
    public async Task FanPerformancesPage_ShowsAudioPlayer_WhenMemberSignedIn()
    {
        var client = await CreateSignedInMemberClientAsync();

        var body = await client.GetStringAsync("/fan-performances");

        Assert.Contains("/fan-performances/187/audio", body);
        Assert.DoesNotContain("Sign in to play", body);
        Assert.DoesNotContain("songfiles", body, StringComparison.OrdinalIgnoreCase);
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
        WebApplicationFactoryClientOptions? options = null)
    {
        var client = factory.CreateClient(options ?? new WebApplicationFactoryClientOptions
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
}
