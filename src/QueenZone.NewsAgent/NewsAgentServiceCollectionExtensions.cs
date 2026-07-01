using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace QueenZone.NewsAgent;

public static class NewsAgentServiceCollectionExtensions
{
    public static IServiceCollection AddQueenZoneNewsAgent(
        this IServiceCollection services,
        IConfiguration? configuration = null)
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

        if (configuration is not null)
        {
            services.AddOptions<OpenRouterOptions>()
                .Bind(configuration.GetSection(OpenRouterOptions.SectionName))
                .PostConfigure(options =>
                {
                    if (string.IsNullOrWhiteSpace(options.ApiKey))
                    {
                        options.ApiKey = configuration["OPENROUTER_API_KEY"];
                    }
                })
                .ValidateOnStart();

            services.AddOptions<NewsTriageOptions>()
                .Bind(configuration.GetSection(NewsTriageOptions.SectionName))
                .ValidateOnStart();
        }
        else
        {
            services.AddOptions<OpenRouterOptions>();
            services.AddOptions<NewsTriageOptions>();
        }

        services.AddSingleton<IValidateOptions<OpenRouterOptions>, OpenRouterOptionsValidator>();
        services.AddSingleton<IValidateOptions<NewsTriageOptions>, NewsTriageOptionsValidator>();
        services.AddHttpClient<INewsAiClient, OpenRouterNewsAiClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<OpenRouterOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(options.RequestTimeoutSeconds);
        });
        services.AddScoped<NewsAiBudgetGuard>();
        services.AddScoped<NewsAiRunExecutor>();
        services.AddScoped<NewsTriageDeterministicAnalyzer>();
        services.AddScoped<NewsTriageService>();

        return services;
    }
}
