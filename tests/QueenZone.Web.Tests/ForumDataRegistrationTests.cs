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

        using var provider = services.BuildServiceProvider();

        Assert.IsType<ModernForumRepository>(provider.GetRequiredService<IForumRepository>());
    }

    [Fact]
    public void AddQueenZoneLegacyData_CanUseLegacyForumRepositoryForRollback()
    {
        var services = new ServiceCollection();

        services.AddQueenZoneLegacyData(
            "Server=(local);Database=QueenZone;Trusted_Connection=True;",
            new ForumDataOptions { UseModernForumReads = false });

        using var provider = services.BuildServiceProvider();

        Assert.IsType<LegacyForumRepository>(provider.GetRequiredService<IForumRepository>());
    }
}
