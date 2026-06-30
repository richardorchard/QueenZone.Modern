using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace QueenZone.Web.Tests;

public sealed class DiscographyPageTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public DiscographyPageTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
    }

    [Fact]
    public async Task DiscographyIndexRendersSeedAlbums()
    {
        var client = factory.CreateClient();

        var body = await client.GetStringAsync("/discography");

        Assert.Contains("Discography", body);
        Assert.Contains("A Night at the Opera", body);
        Assert.Contains("https://pictures.queenzone.org/images/discography/", body);
    }

    [Fact]
    public async Task DiscographyAlbumDetail_RendersTracklist()
    {
        var client = factory.CreateClient();

        var body = await client.GetStringAsync("/discography/albums/4/a-night-at-the-opera");

        Assert.Contains("Bohemian Rhapsody", body);
        Assert.Contains("Seaside Rendezvous", body);
    }

    [Fact]
    public async Task DiscographyAlbumDetail_RedirectsWhenSlugStale()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/discography/albums/4/wrong-slug");

        Assert.Equal(System.Net.HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.Equal("/discography/albums/4/a-night-at-the-opera", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task DiscographyAlbumDetail_ReturnsNotFound_WhenAlbumMissing()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/discography/albums/999/missing");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }
}
