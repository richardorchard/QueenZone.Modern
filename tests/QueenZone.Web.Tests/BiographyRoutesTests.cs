using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using QueenZone.Data;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class BiographyRoutesTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public BiographyRoutesTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
    }

    [Fact]
    public async Task BiographyIndexRendersChaptersInDisplaySequenceOrder()
    {
        var client = factory.CreateClient();

        var body = await client.GetStringAsync("/biography");

        Assert.Contains("Biography", body);
        Assert.Contains("/biography/1/the-formation-of-a-band", body);
        Assert.Contains("/biography/5/legacy", body);
        Assert.Contains("5 chapters", body);
        Assert.Contains("<link rel=\"canonical\" href=\"/biography\">", body);
        Assert.Contains("<title>QueenZone biography</title>", body);

        var formationIndex = body.IndexOf("The Formation of a Band", StringComparison.Ordinal);
        var legacyIndex = body.IndexOf("Legacy", StringComparison.Ordinal);
        Assert.True(formationIndex >= 0);
        Assert.True(legacyIndex > formationIndex);
    }

    [Fact]
    public async Task BiographyDetailRendersChapterBodyAndNavigation()
    {
        var client = factory.CreateClient();

        var body = await client.GetStringAsync("/biography/2/breakthrough");

        Assert.Contains("Breakthrough", body);
        Assert.Contains("Killer Queen", body);
        Assert.Contains("Previous Chapter", body);
        Assert.Contains("Next Chapter", body);
        Assert.Contains("/biography/1/the-formation-of-a-band", body);
        Assert.Contains("/biography/3/a-night-at-the-opera", body);
        Assert.Contains("<link rel=\"canonical\" href=\"/biography/2/breakthrough\">", body);
        Assert.Contains("<title>Breakthrough | QueenZone biography</title>", body);
    }

    [Fact]
    public async Task FirstChapterHidesPreviousNavigation()
    {
        var client = factory.CreateClient();

        var body = await client.GetStringAsync("/biography/1/the-formation-of-a-band");

        Assert.DoesNotContain("Previous Chapter", body);
        Assert.Contains("Next Chapter", body);
    }

    [Fact]
    public async Task LastChapterHidesNextNavigation()
    {
        var client = factory.CreateClient();

        var body = await client.GetStringAsync("/biography/5/legacy");

        Assert.Contains("Previous Chapter", body);
        Assert.DoesNotContain("Next Chapter", body);
    }

    [Fact]
    public async Task MissingBiographyChapterReturnsNotFound()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/biography/999999/does-not-exist");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task WrongBiographySlugRedirectsToCanonicalSlug()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/biography/2/not-the-right-slug");

        Assert.Equal(System.Net.HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.Equal("/biography/2/breakthrough", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task BiographyDetailSanitizesUnsafeLegacyHtmlInBody()
    {
        var chapters = new[]
        {
            new BiographyChapterItem(
                7001,
                "Unsafe HTML chapter",
                "Unsafe summary.",
                "<script>alert('xss')</script><p>Safe <strong>legacy</strong> paragraph</p>",
                1,
                new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc))
        };

        var client = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IBiographyRepository>(new InMemoryBiographyRepository(chapters));
            })).CreateClient();

        var body = await client.GetStringAsync("/biography/7001/unsafe-html-chapter");

        Assert.DoesNotContain("alert", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<p>Safe <strong>legacy</strong> paragraph</p>", body);
    }

    [Fact]
    public async Task EmptyBiographyShowsMessage()
    {
        var client = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IBiographyRepository>(new InMemoryBiographyRepository([]));
            })).CreateClient();

        var body = await client.GetStringAsync("/biography");

        Assert.Contains("No biography chapters are available yet.", body);
    }

    [Theory]
    [InlineData(1, "I")]
    [InlineData(4, "IV")]
    [InlineData(9, "IX")]
    [InlineData(10, "X")]
    public void GetChapterNumeral_ReturnsExpectedRomanNumerals(int index, string expected)
    {
        Assert.Equal(expected, BiographyRoutes.GetChapterNumeral(index - 1));
    }
}