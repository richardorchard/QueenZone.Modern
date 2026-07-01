using Microsoft.Extensions.DependencyInjection;
using QueenZone.Data;
using QueenZone.NewsAgent;

namespace QueenZone.Web.Tests;

public sealed class NewsAgentServiceCollectionTests
{
    [Fact]
    public void AddQueenZoneNewsAgent_registers_discovery_service_and_fetchers()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddQueenZoneInMemoryData();
        services.AddQueenZoneNewsAgent();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var discoveryService = scope.ServiceProvider.GetRequiredService<NewsDiscoveryService>();
        var fetchers = scope.ServiceProvider.GetServices<INewsSourceFetcher>().ToList();

        Assert.NotNull(discoveryService);
        Assert.Equal(3, fetchers.Count);
        Assert.Contains(fetchers, fetcher => fetcher.SourceType == NewsDiscoverySourceType.Rss);
        Assert.Contains(fetchers, fetcher => fetcher.SourceType == NewsDiscoverySourceType.Sitemap);
        Assert.Contains(fetchers, fetcher => fetcher.SourceType == NewsDiscoverySourceType.AllowlistedPage);
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
}
