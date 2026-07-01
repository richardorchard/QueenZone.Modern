using Microsoft.Extensions.DependencyInjection;

namespace QueenZone.NewsAgent;

public static class NewsAgentServiceCollectionExtensions
{
    public static IServiceCollection AddQueenZoneNewsAgent(this IServiceCollection services)
    {
        services.AddHttpClient<INewsDiscoveryHttpClient, NewsDiscoveryHttpClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("QueenZoneNewsDiscovery/1.0");
        })
        .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 10
        });

        services.AddSingleton<NewsSourceFetcherRegistry>();
        services.AddSingleton<INewsSourceFetcher, RssAtomSourceFetcher>();
        services.AddSingleton<INewsSourceFetcher, SitemapSourceFetcher>();
        services.AddSingleton<INewsSourceFetcher, AllowlistedPageSourceFetcher>();
        services.AddScoped<NewsDiscoveryService>();

        return services;
    }
}
