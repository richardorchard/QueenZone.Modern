using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class NewsColumnAvailabilityWarmupServiceTests
{
    [Fact]
    public async Task StartAsync_swallows_probe_failure_and_does_not_block_startup()
    {
        var service = new NewsColumnAvailabilityWarmupService(
            new ThrowingDbContextFactory(),
            NullLogger<NewsColumnAvailabilityWarmupService>.Instance);

        // Must not throw: a failed startup probe is a degraded path, not a boot failure (#1161).
        await service.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_is_registered_by_AddQueenZoneLegacyData()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddQueenZoneLegacyData("Server=(local);Database=QueenZone;Trusted_Connection=True;");

        using var provider = services.BuildServiceProvider();
        var hostedServices = provider.GetServices<Microsoft.Extensions.Hosting.IHostedService>();

        Assert.Contains(hostedServices, s => s is NewsColumnAvailabilityWarmupService);
    }

    private sealed class ThrowingDbContextFactory : IDbContextFactory<QueenZoneDbContext>
    {
        public QueenZoneDbContext CreateDbContext() =>
            throw new InvalidOperationException("Simulated startup probe failure.");

        public Task<QueenZoneDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Simulated startup probe failure.");
    }
}
