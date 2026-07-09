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
}
