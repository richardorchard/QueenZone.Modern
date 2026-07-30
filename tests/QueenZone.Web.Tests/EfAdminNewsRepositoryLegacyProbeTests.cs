using Microsoft.EntityFrameworkCore;
using QueenZone.Data;

namespace QueenZone.Web.Tests;

[Collection(LiveDatabaseProbeCollection.Name)]
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

        var article = all.FirstOrDefault(item => !item.Title.StartsWith("Probe ", StringComparison.OrdinalIgnoreCase))
            ?? all[0];
        var loaded = await repository.GetByIdAsync(article.Id);
        Assert.NotNull(loaded);
        Assert.Equal(article.Id, loaded.Id);
        Assert.Equal(article.Title, loaded.Title);

        var page = await repository.GetPageAsync(1, 5);
        Assert.True(page.TotalCount >= page.Items.Count);
        Assert.NotEmpty(page.Items);
    }
}
