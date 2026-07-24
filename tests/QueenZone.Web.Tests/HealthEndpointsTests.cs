using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using QueenZone.Data;
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
    public async Task Warmup_returns_ok_for_in_memory_sample_data()
    {
        var client = factory.CreateClient();
        using var response = await client.GetAsync("/warmup");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"status\":\"ok\"", body, StringComparison.Ordinal);
        Assert.DoesNotContain("sql", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("blob", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Warmup_failure_returns_minimal_unhealthy_response()
    {
        var appFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<INewsRepository>();
                services.AddSingleton<INewsRepository>(new ThrowingNewsRepository());
            }));
        var client = appFactory.CreateClient();

        using var response = await client.GetAsync("/warmup");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"status\":\"unhealthy\"", body, StringComparison.Ordinal);
        Assert.DoesNotContain("simulated repository failure", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("latest-news", body, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("/health", true)]
    [InlineData("/health/ready", true)]
    [InlineData("/warmup", true)]
    [InlineData("/", false)]
    [InlineData("/news", false)]
    public void IsProbePath_identifies_infrastructure_probe_paths(string path, bool expected)
    {
        Assert.Equal(expected, QueenZoneHealthEndpoints.IsProbePath(path));
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

    private sealed class ThrowingNewsRepository : INewsRepository
    {
        public Task<IReadOnlyList<NewsItem>> GetLatestAsync(int count, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("simulated repository failure");

        public Task<IReadOnlyList<NewsItem>> GetArchivePageAsync(int page, int pageSize, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<NewsItem>>([]);

        public Task<int> GetPublishedCountAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<NewsItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<NewsItem?>(null);

        public Task<IReadOnlyList<SitemapContentEntry>> GetPublishedSitemapEntriesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SitemapContentEntry>>([]);
    }
}
