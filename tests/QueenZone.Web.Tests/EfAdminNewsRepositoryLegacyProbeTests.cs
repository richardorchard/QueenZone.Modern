using Microsoft.EntityFrameworkCore;
using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class EfAdminNewsRepositoryLegacyProbeTests
{
    [Fact]
    public async Task Probe_legacy_admin_news_repository_when_connection_configured()
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__QueenZoneLegacy");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var options = new DbContextOptionsBuilder<QueenZoneDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        await using var dbContext = new QueenZoneDbContext(options);
        var repository = new EfAdminNewsRepository(dbContext);

        var all = await repository.GetAllAsync();
        Assert.NotEmpty(all);

        var article = all[0];
        var loaded = await repository.GetByIdAsync(article.Id);
        Assert.NotNull(loaded);
        Assert.Equal(article.Id, loaded.Id);
        Assert.Equal(article.Title, loaded.Title);

        var promoted = all.FirstOrDefault(a => a.Id > 6900) ?? article;
        var promotedLoaded = await repository.GetByIdAsync(promoted.Id);
        Assert.NotNull(promotedLoaded);
    }
}
