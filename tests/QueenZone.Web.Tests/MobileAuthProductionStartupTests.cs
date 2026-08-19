using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace QueenZone.Web.Tests;

public sealed class MobileAuthProductionStartupTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public MobileAuthProductionStartupTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task ProductionHost_ServesPublicPagesWithoutMobileAuthSigningKey()
    {
        using var productionFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            ResponseCompressionTests.ApplyProductionHostTestSettings(builder);
            builder.UseSetting("MobileAuth:SigningKey", string.Empty);
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["MobileAuth:SigningKey"] = string.Empty,
                });
            });
        });

        var client = productionFactory.CreateClient();

        using var health = await client.GetAsync("/health");
        using var home = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        Assert.Contains("\"status\":\"ok\"", await health.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, home.StatusCode);
    }
}
