using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace QueenZone.Data;

public static class QueenZoneDataServiceCollectionExtensions
{
    public static IServiceCollection AddQueenZoneLegacyData(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<QueenZoneDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddSingleton<INewsRepository>(_ => new LegacyNewsRepository(connectionString));
        services.AddSingleton<IArticlesRepository>(_ => new LegacyArticlesRepository(connectionString));
        services.AddSingleton<IBiographyRepository>(_ => new LegacyBiographyRepository(connectionString));
        services.AddSingleton<IForumRepository>(_ => new LegacyForumRepository(connectionString));
        services.AddSingleton<IPhotoRepository>(_ => new LegacyPhotoRepository(connectionString));
        services.AddScoped<IAdminNewsRepository, EfAdminNewsRepository>();
        services.AddScoped<INewsAuditRepository, EfNewsAuditRepository>();

        return services;
    }

    public static IServiceCollection AddQueenZoneInMemoryData(this IServiceCollection services)
    {
        var store = new SharedNewsStore(SampleNewsData.CreateSeedArticles());
        services.AddSingleton(store);
        services.AddSingleton<INewsRepository, InMemoryNewsRepository>();
        services.AddSingleton<IArticlesRepository>(_ => new InMemoryArticlesRepository(SampleArticlesData.CreateSeedArticles()));
        services.AddSingleton<IBiographyRepository>(_ => new InMemoryBiographyRepository(SampleBiographyData.CreateSeedChapters()));
        services.AddSingleton<IForumRepository>(_ => new InMemoryForumRepository(
            SampleForumData.CreateSeedCategories(),
            SampleForumData.CreateSeedStats()));
        services.AddSingleton<IPhotoRepository>(_ => new InMemoryPhotoRepository(SamplePhotoData.CreateSeedCategories()));
        services.AddSingleton<IAdminNewsRepository, InMemoryAdminNewsRepository>();
        services.AddSingleton<INewsAuditRepository, InMemoryNewsAuditRepository>();

        return services;
    }
}