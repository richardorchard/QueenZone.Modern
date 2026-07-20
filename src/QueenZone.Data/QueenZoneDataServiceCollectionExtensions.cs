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

        services.AddDbContext<QueenZoneDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sql => sql.CommandTimeout(300)));

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
        services.AddScoped<IQueenHistoryRepository, EfQueenHistoryRepository>();
        services.AddScoped<IPhotoSubmissionRepository, EfPhotoSubmissionRepository>();
        services.AddScoped<IArticleSubmissionRepository, EfArticleSubmissionRepository>();
        services.AddScoped<INewsSuggestionRepository, EfNewsSuggestionRepository>();

        return services;
    }

    public static IServiceCollection AddQueenZoneInMemoryData(this IServiceCollection services)
    {
        var store = new SharedNewsStore(SampleNewsData.CreateSeedArticles());
        services.AddSingleton(store);
        services.AddSingleton<INewsRepository, InMemoryNewsRepository>();
        services.AddSingleton<IArticlesRepository>(_ => new InMemoryArticlesRepository(SampleArticlesData.CreateSeedArticles()));
        services.AddSingleton<IBiographyRepository>(_ => new InMemoryBiographyRepository(SampleBiographyData.CreateSeedChapters()));
        var forumWriteRepository = new InMemoryForumWriteRepository();
        var forumAttachmentRepository = new InMemoryForumAttachmentRepository();
        var forumPollRepository = new InMemoryForumPollRepository();
        forumWriteRepository.AttachPollRepository(forumPollRepository);
        services.AddSingleton<IForumWriteRepository>(forumWriteRepository);
        services.AddSingleton<IForumAttachmentRepository>(forumAttachmentRepository);
        services.AddSingleton<IForumPollRepository>(forumPollRepository);
        services.AddSingleton<IForumRepository>(_ => new InMemoryForumRepository(
            SampleForumData.CreateSeedCategories(),
            SampleForumData.CreateSeedStats(),
            forumWriteRepository,
            forumAttachmentRepository));
        services.AddSingleton<IPhotoRepository>(_ => new InMemoryPhotoRepository(SamplePhotoData.CreateSeedCategories()));
        services.AddSingleton<IFanPerformanceRepository>(_ => new InMemoryFanPerformanceRepository(SampleFanPerformanceData.CreateSeedPerformances()));
        services.AddSingleton<ILegacyMemberLookupRepository>(_ => new InMemoryLegacyMemberLookupRepository(SampleLegacyMemberData.CreateSeedMatches()));
        services.AddSingleton<IDiscographyRepository>(_ => new InMemoryDiscographyRepository(SampleDiscographyData.CreateSeedAlbums()));
        services.AddSingleton<IQueenHistoryRepository>(_ => new InMemoryQueenHistoryRepository(SampleQueenHistoryData.CreateSeedEvents()));
        services.AddSingleton<IAdminNewsRepository, InMemoryAdminNewsRepository>();
        services.AddSingleton<INewsAuditRepository, InMemoryNewsAuditRepository>();
        services.AddSingleton<IMemberAccountRepository, InMemoryMemberAccountRepository>();
        var discoveryStore = new SharedNewsDiscoveryStore();
        SampleNewsDiscoveryData.Seed(discoveryStore);
        services.AddSingleton(discoveryStore);
        services.AddSingleton<INewsDiscoveryRepository, InMemoryNewsDiscoveryRepository>();
        services.AddSingleton<SharedNewsAgentLeaseStore>();
        services.AddSingleton<INewsAgentRunLeaseService, InMemoryNewsAgentRunLeaseService>();
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

        services.AddSingleton<IArticleSubmissionRepository>(sp =>
        {
            var members = sp.GetRequiredService<IMemberAccountRepository>();
            return new InMemoryArticleSubmissionRepository(id =>
                members.FindByIdAsync(id).GetAwaiter().GetResult());
        });

        return services;
    }
}
