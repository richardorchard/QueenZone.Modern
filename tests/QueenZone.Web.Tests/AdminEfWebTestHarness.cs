using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QueenZone.Data;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

/// <summary>
/// Wires the web host to SQLite-backed EF admin and discovery repositories so HTTP
/// integration tests exercise real persistence instead of in-memory fakes.
/// </summary>
internal sealed class AdminEfWebTestHarness : IAsyncDisposable
{
    private readonly SqliteConnection connection;

    internal AdminEfWebTestHarness()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
    }

    internal WebApplicationFactory<Program> CreateFactory(
        WebApplicationFactory<Program> baseFactory,
        Action<IServiceCollection>? configureServices = null) =>
        baseFactory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:QueenZoneLegacy", string.Empty);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<QueenZoneDbContext>();
                services.RemoveAll<DbContextOptions<QueenZoneDbContext>>();
                services.RemoveAll<IAdminNewsRepository>();
                services.RemoveAll<INewsAuditRepository>();
                services.RemoveAll<INewsDiscoveryRepository>();
                services.RemoveAll<INewsAgentRunLeaseService>();
                services.RemoveAll<IMemberAccountRepository>();
                services.RemoveAll<SharedNewsDiscoveryStore>();

                services.AddDbContext<QueenZoneDbContext>(options => options.UseSqlite(connection));
                services.AddScoped<IAdminNewsRepository>(sp =>
                {
                    var dbContext = sp.GetRequiredService<QueenZoneDbContext>();
                    return new EfAdminNewsRepository(dbContext, AdminNewsSqliteTestHarness.LatestNewsSql);
                });
                services.AddScoped<INewsAuditRepository, EfNewsAuditRepository>();
                services.AddScoped<INewsDiscoveryRepository, EfNewsDiscoveryRepository>();

                configureServices?.Invoke(services);
            });
        });

    internal void EnsureSchema(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<QueenZoneDbContext>();
        dbContext.Database.EnsureCreated();
        AdminNewsSqliteTestHarness.EnsureNewsTable(dbContext);
    }

    public async ValueTask DisposeAsync() => await connection.DisposeAsync();
}
