using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class BuildStampTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public BuildStampTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
    }

    [Fact]
    public async Task PublicPages_IncludeBuildStampWithMetadataAttributes()
    {
        var client = factory.CreateClient();

        var body = await client.GetStringAsync("/forum");

        Assert.Contains("qz-build-stamp", body);
        Assert.Contains("data-build-version=\"", body);
        Assert.Contains("data-build-utc=\"", body);
        Assert.Contains("toLocaleString", body);
    }

    [Fact]
    public void BuildMetadata_ExposesVersionAndUtcTimestamp()
    {
        Assert.True(BuildMetadata.IsAvailable);
        Assert.False(string.IsNullOrWhiteSpace(BuildMetadata.Version));
        Assert.True(DateTimeOffset.TryParse(BuildMetadata.BuiltAtUtc, out _));
    }
}