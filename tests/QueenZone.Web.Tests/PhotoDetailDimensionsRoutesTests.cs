using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace QueenZone.Web.Tests;

public sealed class PhotoDetailDimensionsRoutesTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public PhotoDetailDimensionsRoutesTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
    }

    [Fact]
    public async Task PhotoDetail_ShowsOriginalDimensionsWhenKnown()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/photography/brian-may/101");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("1920 x 1080", body, StringComparison.Ordinal);
        Assert.Contains("width=\"1920\"", body, StringComparison.Ordinal);
        Assert.Contains("height=\"1080\"", body, StringComparison.Ordinal);
        Assert.DoesNotContain("0 x 0", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PhotoDetail_OmitsZeroDimensions()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/photography/brian-may/103");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("0 x 0", body, StringComparison.Ordinal);
        Assert.DoesNotContain("width=\"0\"", body, StringComparison.Ordinal);
        Assert.Contains("Red Special close-up", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PhotoCategoryGrid_ShowsDimensionsInMetaWhenKnown()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/photography/brian-may");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("1920 x 1080", body, StringComparison.Ordinal);
    }
}
