using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace QueenZone.Data;

public static class QueenZoneDataServiceCollectionExtensions
{
    public static IServiceCollection AddQueenZoneLegacyData(
        this IServiceCollection services,
        string connectionString,
        ForumDataOptions? forumDataOptions = null)
    {
        forumDataOptions ??= new ForumDataOptions();

        // IDbContextFactory first (singleton) for independent short-lived contexts used by
        // parallel admin/reporting reads (issue #335). Scoped QueenZoneDbContext is created
        // from the factory so request-scoped repositories still receive one context per scope.
        // Do not also call AddDbContext with the same options: that re-registers options
        // configuration as scoped and breaks singleton factory resolution.
        services.AddDbContextFactory<QueenZoneDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sql =>
                {
                    sql.CommandTimeout(QueenZoneSqlServerOptions.DefaultCommandTimeoutSeconds);
                    sql.EnableRetryOnFailure(
                        maxRetryCount: QueenZoneSqlServerOptions.MaxRetryCount,
                        maxRetryDelay: QueenZoneSqlServerOptions.MaxRetryDelay,
                        errorNumbersToAdd: null);
                }));
        services.AddScoped(sp =>
            sp.GetRequiredService<IDbContextFactory<QueenZoneDbContext>>().CreateDbContext());

        services.AddScoped<INewsRepository, EfNewsRepository>();
        services.AddScoped<IArticlesRepository, EfArticlesRepository>();
        services.AddScoped<IBiographyRepository, EfBiographyRepository>();
        if (forumDataOptions.UseModernForumReads)
        {
            services.AddScoped<IForumRepository, ModernForumRepository>();
        }
        else
        {
            services.AddScoped<IForumRepository, LegacyForumRepository>();
        }

        services.AddScoped<IPhotoRepository, EfPhotoRepository>();
        services.AddScoped<IAdminPhotoRepository, EfAdminPhotoRepository>();
        services.AddScoped<IFanPerformanceRepository, EfFanPerformanceRepository>();
        services.AddScoped<ILegacyMemberLookupRepository, EfMemberLookupRepository>();
        services.AddScoped<IDiscographyRepository, EfDiscographyRepository>();
        services.AddScoped<IAdminNewsRepository, EfAdminNewsRepository>();
        services.AddScoped<INewsAuditRepository, EfNewsAuditRepository>();
        services.AddScoped<IMemberAccountRepository, EfMemberAccountRepository>();
        services.AddScoped<IForumWriteRepository, EfForumWriteRepository>();
        services.AddScoped<IForumAttachmentRepository, EfForumAttachmentRepository>();
        services.AddScoped<IForumPollRepository, EfForumPollRepository>();
        services.AddScoped<INewsDiscoveryRepository, EfNewsDiscoveryRepository>();
        services.AddScoped<INewsAgentRunLeaseService, EfNewsAgentRunLeaseService>();
        services.AddScoped<INewsAgentRunRequestRepository, EfNewsAgentRunRequestRepository>();
        services.AddScoped<IQueenHistoryRepository, EfQueenHistoryRepository>();
        services.AddScoped<IPhotoSubmissionRepository, EfPhotoSubmissionRepository>();
        services.AddScoped<IArticleSubmissionRepository, EfArticleSubmissionRepository>();
        services.AddScoped<IArticleRepository, EfArticleRepository>();
        services.AddScoped<INewsSuggestionRepository, EfNewsSuggestionRepository>();
        services.AddScoped<IHelpRequestRepository, EfHelpRequestRepository>();
        services.AddScoped<IPrivateMessageRepository, EfPrivateMessageRepository>();
        services.AddScoped<IMemberFollowRepository, EfMemberFollowRepository>();
        services.AddScoped<IMemberPublicActivityRepository, EfMemberPublicActivityRepository>();
        services.AddScoped<ILinksRepository, EfLinksRepository>();
        services.AddScoped<IFreddieTributeRepository, EfFreddieTributeRepository>();
        services.AddScoped<IAdminFreddieTributeRepository, EfAdminFreddieTributeRepository>();
        services.AddScoped<ISearchIndexService, EfSearchIndexService>();
        services.AddScoped<ISiteSearchService, EfSiteSearchService>();
        services.AddScoped<ISearchReindexRunLeaseService, EfSearchReindexRunLeaseService>();
        services.AddScoped<ISearchReindexRunRequestRepository, EfSearchReindexRunRequestRepository>();
        services.AddScoped<IMobileAuthGrantRepository, EfMobileAuthGrantRepository>();
        services.AddScoped<IDeviceTokenRepository, EfDeviceTokenRepository>();
        services.AddScoped<INotificationPreferenceRepository, EfNotificationPreferenceRepository>();
        services.AddScoped<ITopicWatchRepository, EfTopicWatchRepository>();
        services.AddScoped<ITopicWatchLookup>(sp => sp.GetRequiredService<ITopicWatchRepository>());
        services.AddScoped<ILiveActivityQueryService, EfLiveActivityQueryService>();

        return services;
    }

    public static IServiceCollection AddQueenZoneInMemoryData(this IServiceCollection services)
    {
        var store = new SharedNewsStore(SampleNewsData.CreateSeedArticles());
        services.AddSingleton(store);
        services.AddSingleton<INewsRepository, InMemoryNewsRepository>();
        services.AddSingleton<IArticlesRepository>(_ => new InMemoryArticlesRepository(SampleArticlesData.CreateSeedArticles()));
        var biographyStore = new SharedBiographyStore(SampleBiographyData.CreateSeedChapters());
        services.AddSingleton(biographyStore);
        services.AddSingleton<IBiographyRepository>(_ => new InMemoryBiographyRepository(biographyStore));
        var forumWriteRepository = new InMemoryForumWriteRepository();
        var forumAttachmentRepository = new InMemoryForumAttachmentRepository();
        var forumPollRepository = new InMemoryForumPollRepository();
        forumWriteRepository.AttachPollRepository(forumPollRepository);
        services.AddSingleton(forumWriteRepository);
        services.AddSingleton<IForumWriteRepository>(forumWriteRepository);
        services.AddSingleton<IForumAttachmentRepository>(forumAttachmentRepository);
        services.AddSingleton<IForumPollRepository>(forumPollRepository);
        services.AddSingleton<IForumRepository>(_ => new InMemoryForumRepository(
            SampleForumData.CreateSeedCategories(),
            SampleForumData.CreateSeedStats(),
            forumWriteRepository,
            forumAttachmentRepository));
        var photoStore = new SharedPhotoStore(SamplePhotoData.CreateSeedCategories());
        services.AddSingleton(photoStore);
        services.AddSingleton<IPhotoRepository>(_ => new InMemoryPhotoRepository(photoStore));
        services.AddSingleton<IAdminPhotoRepository>(_ => new InMemoryAdminPhotoRepository(photoStore));
        services.AddSingleton<IFanPerformanceRepository>(_ => new InMemoryFanPerformanceRepository(SampleFanPerformanceData.CreateSeedPerformances()));
        services.AddSingleton<ILegacyMemberLookupRepository>(_ => new InMemoryLegacyMemberLookupRepository(SampleLegacyMemberData.CreateSeedMatches()));
        services.AddSingleton<IDiscographyRepository>(_ => new InMemoryDiscographyRepository(SampleDiscographyData.CreateSeedAlbums()));
        services.AddSingleton<IQueenHistoryRepository>(_ => new InMemoryQueenHistoryRepository(SampleQueenHistoryData.CreateSeedEvents()));
        services.AddSingleton<IAdminNewsRepository, InMemoryAdminNewsRepository>();
        services.AddSingleton<INewsAuditRepository, InMemoryNewsAuditRepository>();
        services.AddSingleton<IMemberAccountRepository, InMemoryMemberAccountRepository>();
        services.AddSingleton<ILiveActivityQueryService, InMemoryLiveActivityQueryService>();
        var discoveryStore = new SharedNewsDiscoveryStore();
        SampleNewsDiscoveryData.Seed(discoveryStore);
        services.AddSingleton(discoveryStore);
        services.AddSingleton<INewsDiscoveryRepository, InMemoryNewsDiscoveryRepository>();
        services.AddSingleton<SharedNewsAgentLeaseStore>();
        services.AddSingleton<INewsAgentRunLeaseService, InMemoryNewsAgentRunLeaseService>();
        services.AddSingleton<SharedNewsAgentRunRequestStore>();
        services.AddSingleton<INewsAgentRunRequestRepository, InMemoryNewsAgentRunRequestRepository>();
        services.AddSingleton<IPhotoSubmissionRepository>(sp =>
        {
            var members = sp.GetRequiredService<IMemberAccountRepository>();
            return new InMemoryPhotoSubmissionRepository(id =>
                members.FindByIdAsync(id).GetAwaiter().GetResult());
        });
        services.AddSingleton<INewsSuggestionRepository>(sp =>
        {
            var members = sp.GetRequiredService<IMemberAccountRepository>();
            return new InMemoryNewsSuggestionRepository(id =>
                members.FindByIdAsync(id).GetAwaiter().GetResult());
        });
        services.AddSingleton<IHelpRequestRepository, InMemoryHelpRequestRepository>();
        services.AddSingleton<IPrivateMessageRepository>(sp =>
        {
            var members = sp.GetRequiredService<IMemberAccountRepository>();
            return new InMemoryPrivateMessageRepository(id =>
                members.FindByIdAsync(id).GetAwaiter().GetResult());
        });
        services.AddSingleton<IMemberFollowRepository, InMemoryMemberFollowRepository>();

        services.AddSingleton<IArticleSubmissionRepository>(sp =>
        {
            var members = sp.GetRequiredService<IMemberAccountRepository>();
            return new InMemoryArticleSubmissionRepository(id =>
                members.FindByIdAsync(id).GetAwaiter().GetResult());
        });
        services.AddSingleton<IArticleRepository>(sp =>
            new InMemoryArticleRepository(sp.GetRequiredService<IArticleSubmissionRepository>()));
        services.AddSingleton<IMemberPublicActivityRepository, InMemoryMemberPublicActivityRepository>();
        services.AddSingleton<ILinksRepository>(_ => new InMemoryLinksRepository(SampleLinksData.CreateSeedCategories()));
        services.AddSingleton(_ => new SharedFreddieTributeStore(SampleFreddieTributeData.CreateSeedTributes()));
        services.AddSingleton<IFreddieTributeRepository, InMemoryFreddieTributeRepository>();
        services.AddSingleton<IAdminFreddieTributeRepository, InMemoryAdminFreddieTributeRepository>();
        services.AddSingleton<SharedSearchIndexStore>();
        services.AddSingleton<ISearchIndexService, InMemorySearchIndexService>();
        services.AddSingleton<ISiteSearchService, InMemorySiteSearchService>();
        services.AddSingleton<SharedSearchReindexLeaseStore>();
        services.AddSingleton<ISearchReindexRunLeaseService, InMemorySearchReindexRunLeaseService>();
        services.AddSingleton<SharedSearchReindexRunRequestStore>();
        services.AddSingleton<ISearchReindexRunRequestRepository, InMemorySearchReindexRunRequestRepository>();
        services.AddSingleton<SharedMobileAuthGrantStore>();
        services.AddSingleton<IMobileAuthGrantRepository, InMemoryMobileAuthGrantRepository>();
        services.AddSingleton<SharedDeviceTokenStore>();
        services.AddSingleton<IDeviceTokenRepository, InMemoryDeviceTokenRepository>();
        services.AddSingleton<SharedNotificationPreferenceStore>();
        services.AddSingleton<INotificationPreferenceRepository, InMemoryNotificationPreferenceRepository>();
        services.AddSingleton<ITopicWatchRepository, InMemoryTopicWatchRepository>();
        services.AddSingleton<ITopicWatchLookup>(sp => sp.GetRequiredService<ITopicWatchRepository>());

        return services;
    }
}

