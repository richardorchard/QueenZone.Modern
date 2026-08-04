using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace QueenZone.NewsAgent;

/// <summary>
/// DI registration for news-agent services.
/// Prefer the host-specific methods: <see cref="AddQueenZoneNewsAgentWeb"/> for the
/// public web app (admin draft regenerate only) and
/// <see cref="AddQueenZoneNewsAgentWorker"/> for discovery/triage/draft execution.
/// </summary>
public static class NewsAgentServiceCollectionExtensions
{
    /// <summary>
    /// Registers OpenRouter + draft generation used by admin review UI
    /// (regenerate draft). Does <strong>not</strong> register outbound discovery
    /// HTTP clients, source fetchers, triage, scheduler options, or
    /// <see cref="DiscoverNewsWorker"/>.
    /// </summary>
    public static IServiceCollection AddQueenZoneNewsAgentWeb(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        RegisterOpenRouterOptions(services, configuration);
        RegisterDraftOptions(services, configuration);
        RegisterOpenRouterAiClient(services);
        services.AddScoped<NewsAiBudgetGuard>();
        services.AddScoped<NewsAiRunExecutor>();
        services.AddScoped<NewsDraftGenerationService>();
        return services;
    }

    /// <summary>
    /// Registers the full discovery pipeline for the console worker:
    /// source fetchers, discovery service, triage, draft generation, and
    /// <see cref="DiscoverNewsWorker"/>.
    /// </summary>
    public static IServiceCollection AddQueenZoneNewsAgentWorker(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        RegisterDiscoveryFetch(services);
        RegisterTriageOptions(services, configuration);
        RegisterSchedulerOptions(services, configuration);
        // Editorial AI (shared with web admin regenerate).
        AddQueenZoneNewsAgentWeb(services, configuration);

        services.AddScoped<NewsTriageDeterministicAnalyzer>();
        services.AddScoped<NewsTriageService>();
        services.AddScoped<NewsAgentUrlIngestionService>();
        services.AddScoped<DiscoverNewsWorker>();
        services.AddScoped<INewsAgentQueuedRunExecutor, NewsAgentQueuedRunExecutor>();
        services.AddScoped<NewsAgentQueuedRunProcessor>();
        return services;
    }

    /// <summary>
    /// Full pipeline registration. Prefer <see cref="AddQueenZoneNewsAgentWorker"/>
    /// or <see cref="AddQueenZoneNewsAgentWeb"/> at host boundaries.
    /// Kept for tests and call sites that want the complete worker surface.
    /// </summary>
    public static IServiceCollection AddQueenZoneNewsAgent(
        this IServiceCollection services,
        IConfiguration? configuration = null) =>
        AddQueenZoneNewsAgentWorker(services, configuration);

    private static void RegisterDiscoveryFetch(IServiceCollection services)
    {
        services.AddHttpClient<INewsDiscoveryHttpClient, NewsDiscoveryHttpClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("QueenZoneNewsDiscovery/1.0");
        })
        // SSRF: block private/link-local/metadata destinations after DNS (and on redirects).
        .ConfigurePrimaryHttpMessageHandler(() => SsrfSafeSocketsHttpHandler.Create(maxAutomaticRedirections: 5));

        services.AddSingleton<NewsSourceFetcherRegistry>();
        services.AddSingleton<INewsSourceFetcher, RssAtomSourceFetcher>();
        services.AddSingleton<INewsSourceFetcher, SitemapSourceFetcher>();
        services.AddSingleton<INewsSourceFetcher, AllowlistedPageSourceFetcher>();
        services.AddScoped<NewsDiscoveryService>();
    }

    private static void RegisterOpenRouterOptions(IServiceCollection services, IConfiguration? configuration)
    {
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

                    options.ApiKey = OpenRouterOptions.NormalizeApiKey(options.ApiKey);
                })
                .ValidateOnStart();
        }
        else
        {
            services.AddOptions<OpenRouterOptions>();
        }

        services.AddSingleton<IValidateOptions<OpenRouterOptions>, OpenRouterOptionsValidator>();
    }

    private static void RegisterDraftOptions(IServiceCollection services, IConfiguration? configuration)
    {
        if (configuration is not null)
        {
            services.AddOptions<NewsDraftGenerationOptions>()
                .Bind(configuration.GetSection(NewsDraftGenerationOptions.SectionName))
                .ValidateOnStart();
        }
        else
        {
            services.AddOptions<NewsDraftGenerationOptions>();
        }

        services.AddSingleton<IValidateOptions<NewsDraftGenerationOptions>, NewsDraftGenerationOptionsValidator>();
    }

    private static void RegisterTriageOptions(IServiceCollection services, IConfiguration? configuration)
    {
        if (configuration is not null)
        {
            services.AddOptions<NewsTriageOptions>()
                .Bind(configuration.GetSection(NewsTriageOptions.SectionName))
                .ValidateOnStart();
        }
        else
        {
            services.AddOptions<NewsTriageOptions>();
        }

        services.AddSingleton<IValidateOptions<NewsTriageOptions>, NewsTriageOptionsValidator>();
    }

    private static void RegisterSchedulerOptions(IServiceCollection services, IConfiguration? configuration)
    {
        if (configuration is not null)
        {
            services.AddOptions<NewsAgentSchedulerOptions>()
                .Bind(configuration.GetSection(NewsAgentSchedulerOptions.SectionName));
        }
        else
        {
            services.AddOptions<NewsAgentSchedulerOptions>();
        }
    }

    private static void RegisterOpenRouterAiClient(IServiceCollection services)
    {
        services.AddHttpClient<INewsAiClient, OpenRouterNewsAiClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<OpenRouterOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(options.RequestTimeoutSeconds);
        });
    }
}
