using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using QueenZone.Data;
using QueenZone.NewsAgent;

namespace QueenZone.Web.Tests;

public sealed class NewsAgentServiceCollectionTests
{
    [Fact]
    public void AddQueenZoneNewsAgentWeb_registers_draft_ai_but_not_discovery_pipeline()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddQueenZoneInMemoryData();
        services.AddQueenZoneNewsAgentWeb();

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;

        Assert.NotNull(sp.GetRequiredService<NewsDraftGenerationService>());
        Assert.NotNull(sp.GetRequiredService<INewsAgentGuidanceProvider>());
        Assert.NotNull(sp.GetRequiredService<NewsAiRunExecutor>());
        Assert.NotNull(sp.GetRequiredService<INewsAiClient>());
        Assert.False(sp.GetRequiredService<INewsAiClient>().IsEnabled);

        Assert.Null(sp.GetService<NewsDiscoveryService>());
        Assert.Null(sp.GetService<DiscoverNewsWorker>());
        Assert.Null(sp.GetService<NewsTriageService>());
        Assert.Null(sp.GetService<NewsTriageDeterministicAnalyzer>());
        Assert.NotNull(sp.GetRequiredService<INewsDiscoveryHttpClient>());
        Assert.Empty(sp.GetServices<INewsSourceFetcher>());
        // Options containers may resolve defaults; the worker-only services above are the surface check.
    }

    [Fact]
    public void AddQueenZoneNewsAgentWorker_registers_full_discovery_pipeline()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddQueenZoneInMemoryData();
        services.AddQueenZoneNewsAgentWorker();

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;

        var discoveryService = sp.GetRequiredService<NewsDiscoveryService>();
        var fetchers = sp.GetServices<INewsSourceFetcher>().ToList();
        Assert.NotNull(discoveryService);
        Assert.Equal(3, fetchers.Count);
        Assert.Contains(fetchers, fetcher => fetcher.SourceType == NewsDiscoverySourceType.Rss);
        Assert.Contains(fetchers, fetcher => fetcher.SourceType == NewsDiscoverySourceType.Sitemap);
        Assert.Contains(fetchers, fetcher => fetcher.SourceType == NewsDiscoverySourceType.AllowlistedPage);

        Assert.NotNull(sp.GetRequiredService<DiscoverNewsWorker>());
        Assert.NotNull(sp.GetRequiredService<NewsTriageService>());
        Assert.NotNull(sp.GetRequiredService<INewsAgentGuidanceProvider>());
        Assert.NotNull(sp.GetRequiredService<NewsDraftGenerationService>());
        Assert.NotNull(sp.GetRequiredService<INewsDiscoveryHttpClient>());
        Assert.NotNull(sp.GetRequiredService<IOptions<NewsTriageOptions>>());
        Assert.NotNull(sp.GetRequiredService<IOptions<NewsAgentSchedulerOptions>>());
    }

    [Fact]
    public void AddQueenZoneNewsAgent_alias_matches_worker_surface()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddQueenZoneInMemoryData();
        services.AddQueenZoneNewsAgent();

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<DiscoverNewsWorker>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<NewsDiscoveryService>());
    }

    [Fact]
    public void AddQueenZoneNewsAgent_registers_openrouter_ai_services()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddQueenZoneInMemoryData();
        services.AddQueenZoneNewsAgent();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var aiClient = scope.ServiceProvider.GetRequiredService<INewsAiClient>();
        var executor = scope.ServiceProvider.GetRequiredService<NewsAiRunExecutor>();

        Assert.False(aiClient.IsEnabled);
        Assert.False(executor.IsAiEnabled);
    }

    [Fact]
    public void AddQueenZoneNewsAgent_normalizes_openrouter_api_key_from_configuration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OPENROUTER_API_KEY"] = "  \"sk-or-v1-test\"  "
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddQueenZoneInMemoryData();
        services.AddQueenZoneNewsAgent(configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<OpenRouterOptions>>().Value;

        Assert.Equal("sk-or-v1-test", options.ApiKey);
        Assert.True(options.IsConfigured);
    }
}
