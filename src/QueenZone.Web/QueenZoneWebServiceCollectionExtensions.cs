using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using QueenZone.Data;
using QueenZone.NewsAgent;
using QueenZone.Storage;
using QueenZone.Web.Health;
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

        services.AddOptions<ForumOptions>()
            .Bind(configuration.GetSection(ForumOptions.SectionName));

        services.AddOptions<PublicQueryCacheOptions>()
            .Bind(configuration.GetSection(PublicQueryCacheOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<PublicQueryCacheOptions>, PublicQueryCacheOptionsValidator>();

        services.AddOptions<ForumAttachmentOptions>()
            .Bind(configuration.GetSection(ForumAttachmentOptions.SectionName));

        services.AddOptions<NewsSuggestionOptions>()
            .Bind(configuration.GetSection(NewsSuggestionOptions.SectionName));

        return services;
    }

    public static IServiceCollection AddQueenZoneRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<FanPerformanceRateLimitingOptions>()
            .Bind(configuration.GetSection(FanPerformanceRateLimitingOptions.SectionName));

        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            limiter.AddPolicy(FanPerformanceRateLimitingOptions.AudioPolicy, context =>
            {
                var opts = context.RequestServices
                    .GetRequiredService<IOptions<FanPerformanceRateLimitingOptions>>().Value;
                return RateLimitPartition.GetSlidingWindowLimiter(
                    context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous",
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = opts.AudioPermitLimit,
                        Window = TimeSpan.FromSeconds(opts.AudioSlidingWindowSeconds),
                        SegmentsPerWindow = Math.Max(1, opts.AudioSlidingWindowSeconds / 60),
                        QueueLimit = 0,
                    });
            });

            limiter.AddPolicy(FanPerformanceRateLimitingOptions.BrowsePolicy, context =>
            {
                var opts = context.RequestServices
                    .GetRequiredService<IOptions<FanPerformanceRateLimitingOptions>>().Value;
                return RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = opts.BrowsePermitLimit,
                        Window = TimeSpan.FromSeconds(opts.BrowseWindowSeconds),
                        QueueLimit = 0,
                    });
            });
        });

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

            options.AddPolicy(PublicOutputCachePolicies.PublicHtml, policy => policy
                .With(context => PublicOutputCachePolicies.IsCacheablePublicHtmlRequest(context.HttpContext))
                .Expire(PublicOutputCachePolicies.HtmlDuration)
                .SetVaryByRouteValue("*")
                .SetVaryByQuery("*")
                .Tag(PublicOutputCachePolicies.PublicHtmlTag));
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
        services.AddScoped<PhotoSubmissionService>();
        services.AddScoped<NewsSuggestionService>();
        services.AddScoped<UgcHtml>();
        services.AddScoped<ForumPostRateLimiter>();
        services.AddScoped<ForumAttachmentValidator>();
        services.AddScoped<ForumAttachmentUploadService>();
        services.AddSingleton<IGoogleAnalyticsDataClient, GoogleAnalyticsDataClient>();
        services.AddScoped<IGoogleAnalyticsTrafficService, GoogleAnalyticsTrafficService>();
        services.AddScoped<AdminDashboardService>();
        // Header name used by the rich-text editor fetch() upload helper.
        services.AddAntiforgery(options =>
        {
            options.HeaderName = EditorImageUploadEndpoints.AntiforgeryHeaderName;
        });
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
        services.AddQueenZoneRateLimiting(configuration);
        services.AddQueenZoneSitemaps();
        services.AddQueenZoneWebAppServices();

        if (ResponseCompressionBootstrap.IsEnabled(environment))
        {
            ResponseCompressionBootstrap.ConfigureServices(services);
        }

        services.AddQueenZoneData(configuration);
        services.AddQueenZoneStorage(configuration);
        services.AddQueenZoneHealthChecks();
        services.AddQueenZoneNewsAgent(configuration);
        services.AddQueenZoneAuth(configuration, environment);
        services.AddQueenZoneAuthorization(configuration, environment);

        return services;
    }
}
