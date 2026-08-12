using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using QueenZone.Data;
using QueenZone.Web.Search;

namespace QueenZone.Web.Tests;

public sealed class SearchReindexWorkerServiceCollectionExtensionsTests
{
    [Fact]
    public void AddQueenZoneSearchReindexWorker_binds_scheduler_options_from_configuration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SearchReindexScheduler:LeaseName"] = "custom-lease",
                ["SearchReindexScheduler:LeaseDurationMinutes"] = "15",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddQueenZoneInMemoryData();
        services.AddQueenZoneSearchReindexWorker(configuration);

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;

        var options = sp.GetRequiredService<IOptions<SearchReindexSchedulerOptions>>().Value;
        Assert.Equal("custom-lease", options.LeaseName);
        Assert.Equal(15, options.LeaseDurationMinutes);

        Assert.NotNull(sp.GetRequiredService<SearchReindexBuilder>());
        Assert.NotNull(sp.GetRequiredService<SearchReindexScheduledWorker>());
    }

    [Fact]
    public void AddQueenZoneSearchReindexWorker_uses_defaults_without_configuration()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddQueenZoneInMemoryData();
        services.AddQueenZoneSearchReindexWorker();

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<SearchReindexSchedulerOptions>>().Value;

        Assert.Equal("search-reindex", options.LeaseName);
        Assert.True(options.UseRunLease);
    }
}
