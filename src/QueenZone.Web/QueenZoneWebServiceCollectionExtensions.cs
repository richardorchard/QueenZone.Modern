using Microsoft.Extensions.Options;
using QueenZone.Data;
using QueenZone.NewsAgent;
using QueenZone.Storage;
using QueenZone.Web.Sitemap;

namespace QueenZone.Web;

public static class QueenZoneWebServiceCollectionExtensions
{
    public static IServiceCollection AddQueenZoneWebOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<AdminOptions>()
            .Bind(configuration.GetSection(AdminOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<AdminOptions>, AdminOptionsValidator>();

        services.AddOptions<SiteOptions>()
            .Bind(configuration.GetSection(SiteOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<SiteOptions>, SiteOptionsValidator>();

        services.AddOptions<AnalyticsOptions>()
            .Bind(configuration.GetSection(AnalyticsOptions.SectionName));

        services.AddOptions<SitemapOptions>()
            .Bind(configuration.GetSection(SitemapOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<SitemapOptions>, SitemapOptionsValidator>();

        services.AddOptions<MemberAuthenticationOptions>()
            .Bind(configuration.GetSection(MemberAuthenticationOptions.SectionName));

        services.AddOptions<ForumDataOptions>()
            .Bind(configuration.GetSection(ForumDataOptions.SectionName));

        services.AddOptions<PublicQueryCacheOptions>()
            .Bind(configuration.GetSection(PublicQueryCacheOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<PublicQueryCacheOptions>, PublicQueryCacheOptionsValidator>();

        return services;
    }

    public static IServiceCollection AddQueenZoneCaching(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddOutputCache(options =>
        {
            options.AddPolicy(PublicOutputCachePolicies.PublicSitemaps, policy => policy
                .With(context => PublicOutputCachePolicies.IsPublicReadOnlyRequest(context.HttpContext))
                .Expire(PublicOutputCachePolicies.SitemapDuration)
                .SetVaryByRouteValue("*")
                .Tag(PublicOutputCachePolicies.PublicSitemapTag));
        });
        services.AddScoped<PublicQueryCacheService>();
        return services;
    }

    public static IServiceCollection AddQueenZoneSitemaps(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        // Scoped: sitemap builders depend on EF-backed content repositories (scoped DbContext).
        services.AddScoped<CoreSitemapBuilder>();
        services.AddScoped<CoreSitemapService>();
        services.AddScoped<ForumSitemapBuilder>();
        services.AddScoped<SitemapIndexBuilder>();
        return services;
    }

    public static IServiceCollection AddQueenZoneWebAppServices(this IServiceCollection services)
    {
        services.AddScoped<MemberAccountService>();
        services.AddAntiforgery();
        return services;
    }

    public static IServiceCollection AddQueenZoneData(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var legacyConnectionString = configuration.GetConnectionString("QueenZoneLegacy");
        if (!string.IsNullOrWhiteSpace(legacyConnectionString))
        {
            var forumDataOptions = configuration
                .GetSection(ForumDataOptions.SectionName)
                .Get<ForumDataOptions>() ?? new ForumDataOptions();

            services.AddQueenZoneLegacyData(legacyConnectionString, forumDataOptions);
        }
        else
        {
            services.AddQueenZoneInMemoryData();
        }

        return services;
    }

    public static IServiceCollection AddQueenZoneWebComposition(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddQueenZoneWebOptions(configuration);
        services.AddQueenZoneCaching();
        services.AddQueenZoneSitemaps();
        services.AddQueenZoneWebAppServices();

        if (ResponseCompressionBootstrap.IsEnabled(environment))
        {
            ResponseCompressionBootstrap.ConfigureServices(services);
        }

        services.AddQueenZoneData(configuration);
        services.AddQueenZoneStorage(configuration);
        services.AddQueenZoneNewsAgent(configuration);
        services.AddQueenZoneAuth(configuration, environment);
        services.AddQueenZoneAuthorization(configuration);

        return services;
    }
}
