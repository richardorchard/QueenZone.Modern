using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class ForumDataRegistrationTests
{
    [Fact]
    public void AddQueenZoneLegacyData_UsesModernForumRepositoryByDefault()
    {
        var services = new ServiceCollection();

        services.AddQueenZoneLegacyData("Server=(local);Database=QueenZone;Trusted_Connection=True;");

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        Assert.IsType<ModernForumRepository>(scope.ServiceProvider.GetRequiredService<IForumRepository>());
    }

    [Fact]
    public void AddQueenZoneLegacyData_CanUseLegacyForumRepositoryForRollback()
    {
        var services = new ServiceCollection();

        services.AddQueenZoneLegacyData(
            "Server=(local);Database=QueenZone;Trusted_Connection=True;",
            new ForumDataOptions { UseModernForumReads = false });

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        Assert.IsType<LegacyForumRepository>(scope.ServiceProvider.GetRequiredService<IForumRepository>());
    }

    [Fact]
    public void AddQueenZoneLegacyData_RegistersDbContextFactory_ForParallelSafeReads()
    {
        var services = new ServiceCollection();

        services.AddQueenZoneLegacyData("Server=(local);Database=QueenZone;Trusted_Connection=True;");

        using var provider = services.BuildServiceProvider(validateScopes: true);
        var factory = provider.GetService<IDbContextFactory<QueenZoneDbContext>>();

        Assert.NotNull(factory);
        using var context1 = factory.CreateDbContext();
        using var context2 = factory.CreateDbContext();
        Assert.NotSame(context1, context2);

        using var scope = provider.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetService<QueenZoneDbContext>());
    }

    [Fact]
    public void AddQueenZoneInMemoryData_DoesNotRegisterDbContextFactory()
    {
        var services = new ServiceCollection();

        services.AddQueenZoneInMemoryData();

        using var provider = services.BuildServiceProvider(validateScopes: true);

        Assert.Null(provider.GetService<IDbContextFactory<QueenZoneDbContext>>());
        Assert.Null(provider.GetService<QueenZoneDbContext>());
    }
}
