using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QueenZone.Data;
using QueenZone.Data.Entities;

namespace QueenZone.Web.Tests;

public sealed class EfLinksRepositoryUpsertTests : IAsyncDisposable
{
    private readonly SqliteConnection connection = new("DataSource=:memory:");
    private readonly QueenZoneDbContext dbContext;
    private readonly EfLinksRepository repository;

    public EfLinksRepositoryUpsertTests()
    {
        connection.Open();
        var options = new DbContextOptionsBuilder<QueenZoneDbContext>()
            .UseSqlite(connection)
            .Options;
        dbContext = new QueenZoneDbContext(options);
        dbContext.Database.EnsureCreated();
        // Bypass production SQL (legacy Q_LINK tables) — only UpsertCheckResultsAsync is under test.
        repository = new EfLinksRepository(dbContext, publicLinksSql: "SELECT 1 WHERE 1 = 0", validationLinksSql: "SELECT 1 WHERE 1 = 0");
    }

    [Fact]
    public async Task UpsertCheckResultsAsync_inserts_and_updates_in_single_save()
    {
        await repository.UpsertCheckResultsAsync(
        [
            new QueenLinkCheckUpdate(10, "https://a.example/", DateTime.UtcNow, true, false, 0, 200, null),
            new QueenLinkCheckUpdate(11, "https://b.example/", DateTime.UtcNow, false, false, 1, 404, "not found"),
        ]);

        Assert.Equal(2, await dbContext.QueenLinkChecks.CountAsync());
        var first = await dbContext.QueenLinkChecks.SingleAsync(e => e.QueenFeaturedSiteId == 10);
        Assert.True(first.IsAvailable);
        Assert.Equal(200, first.LastStatusCode);

        await repository.UpsertCheckResultsAsync(
        [
            new QueenLinkCheckUpdate(10, "https://a.example/", DateTime.UtcNow, false, true, 3, 500, "down"),
            new QueenLinkCheckUpdate(12, "https://c.example/", DateTime.UtcNow, true, false, 0, 200, null),
        ]);

        Assert.Equal(3, await dbContext.QueenLinkChecks.CountAsync());
        first = await dbContext.QueenLinkChecks.SingleAsync(e => e.QueenFeaturedSiteId == 10);
        Assert.False(first.IsAvailable);
        Assert.True(first.IsConfirmedDead);
        Assert.Equal(3, first.ConsecutiveFailureCount);
        Assert.Equal(500, first.LastStatusCode);
        Assert.Equal("down", first.LastError);
        Assert.True(await dbContext.QueenLinkChecks.AnyAsync(e => e.QueenFeaturedSiteId == 12));
    }

    [Fact]
    public async Task UpsertCheckResultsAsync_noops_for_empty_batch()
    {
        await repository.UpsertCheckResultsAsync([]);
        Assert.Equal(0, await dbContext.QueenLinkChecks.CountAsync());
    }

    public async ValueTask DisposeAsync()
    {
        await dbContext.DisposeAsync();
        await connection.DisposeAsync();
    }
}
