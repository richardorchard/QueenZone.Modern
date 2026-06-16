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
        services.AddScoped<IAdminNewsRepository, EfAdminNewsRepository>();
        services.AddScoped<INewsAuditRepository, EfNewsAuditRepository>();

        return services;
    }

    public static IServiceCollection AddQueenZoneInMemoryData(this IServiceCollection services)
    {
        var store = new SharedNewsStore(SampleNewsData.CreateSeedArticles());
        services.AddSingleton(store);
        services.AddSingleton<INewsRepository, InMemoryNewsRepository>();
        services.AddSingleton<IAdminNewsRepository, InMemoryAdminNewsRepository>();
        services.AddSingleton<INewsAuditRepository, InMemoryNewsAuditRepository>();

        return services;
    }
}