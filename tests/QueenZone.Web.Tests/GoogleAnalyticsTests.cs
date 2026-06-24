using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class GoogleAnalyticsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string MeasurementId = "G-V2W56BZ3KZ";
    private readonly WebApplicationFactory<Program> factory;

    public GoogleAnalyticsTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
    }

    [Fact]
    public async Task PublicPages_OmitGoogleAnalyticsInTestingEnvironment()
    {
        var client = factory.CreateClient();

        var body = await client.GetStringAsync("/forum");

        Assert.DoesNotContain("googletagmanager.com/gtag/js", body);
        Assert.DoesNotContain(MeasurementId, body);
    }

    [Fact]
    public async Task PublicPages_IncludeGoogleAnalyticsWhenConfigured()
    {
        var client = CreateClientWithMeasurementId();

        var body = await client.GetStringAsync("/forum");

        Assert.Contains($"https://www.googletagmanager.com/gtag/js?id={MeasurementId}", body);
        Assert.Contains($"gtag('config', '{MeasurementId}');", body);
    }

    [Fact]
    public async Task AdminPages_OmitGoogleAnalyticsEvenWhenConfigured()
    {
        var client = CreateClientWithMeasurementId();

        var response = await client.GetAsync("/Admin/News");

        var body = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("googletagmanager.com/gtag/js", body);
        Assert.DoesNotContain(MeasurementId, body);
    }

    private HttpClient CreateClientWithMeasurementId() =>
        factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [$"{AnalyticsOptions.SectionName}:MeasurementId"] = MeasurementId
                });
            })).CreateClient();
}