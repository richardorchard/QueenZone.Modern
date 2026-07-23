using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using QueenZone.Storage;
using QueenZone.Web.Health;

namespace QueenZone.Web.Tests;

public sealed class HealthEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public HealthEndpointsTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
    }

    [Fact]
    public async Task Liveness_health_returns_ok_without_dependency_details()
    {
        var client = factory.CreateClient();
        using var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"status\":\"ok\"", body, StringComparison.Ordinal);
        Assert.DoesNotContain("sql", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("blob", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Readiness_health_is_healthy_for_in_memory_sample_data()
    {
        var client = factory.CreateClient();
        using var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        Assert.Equal("Healthy", doc.RootElement.GetProperty("status").GetString());

        var entries = doc.RootElement.GetProperty("entries");
        Assert.Equal("Healthy", entries.GetProperty("sql").GetProperty("status").GetString());
        Assert.Equal("Healthy", entries.GetProperty("blob").GetProperty("status").GetString());
        Assert.Contains(
            "not configured",
            entries.GetProperty("sql").GetProperty("description").GetString(),
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains(
            "not configured",
            entries.GetProperty("blob").GetProperty("description").GetString(),
            StringComparison.OrdinalIgnoreCase);

        // No connection-string shaped secrets in the payload.
        var raw = doc.RootElement.GetRawText();
        Assert.DoesNotContain("Password=", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AccountKey=", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SqlReadyHealthCheck_without_dbcontext_is_healthy()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        await using var provider = services.BuildServiceProvider();
        var check = new SqlReadyHealthCheck(provider.GetRequiredService<IServiceScopeFactory>());
        var result = await check.CheckHealthAsync(new HealthCheckContext());
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Contains("not configured", result.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BlobReadyHealthCheck_with_null_service_is_healthy()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IBlobUploadService, NullBlobUploadService>();
        await using var provider = services.BuildServiceProvider();
        var check = new BlobReadyHealthCheck(
            provider.GetRequiredService<IBlobUploadService>(),
            provider);
        var result = await check.CheckHealthAsync(new HealthCheckContext());
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Contains("not configured", result.Description, StringComparison.OrdinalIgnoreCase);
    }
}
