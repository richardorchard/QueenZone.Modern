using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QueenZone.Data;
using QueenZone.Data.Entities;

namespace QueenZone.Web.Tests;

public sealed class EfNewsAgentRunLeaseServiceTests : IAsyncDisposable
{
    private readonly SqliteConnection connection;
    private readonly QueenZoneDbContext dbContext;
    private readonly EfNewsAgentRunLeaseService service;

    public EfNewsAgentRunLeaseServiceTests()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<QueenZoneDbContext>()
            .UseSqlite(connection)
            .Options;
        dbContext = new QueenZoneDbContext(options);
        dbContext.Database.EnsureCreated();
        service = new EfNewsAgentRunLeaseService(dbContext);
    }

    [Fact]
    public async Task TryAcquireAsync_inserts_lease_when_none_exists()
    {
        await using var lease = (await service.TryAcquireAsync("discover-news", TimeSpan.FromMinutes(30)))!;

        Assert.NotNull(lease);
        Assert.Equal("discover-news", lease.LeaseName);
        Assert.False(string.IsNullOrWhiteSpace(lease.HolderId));

        var stored = await dbContext.NewsAgentRunLeases.SingleAsync();
        Assert.Equal(lease.HolderId, stored.HolderId);
    }

    [Fact]
    public async Task TryAcquireAsync_returns_null_when_another_holder_holds_active_lease()
    {
        await using var first = (await service.TryAcquireAsync("discover-news", TimeSpan.FromMinutes(30)))!;

        var second = await service.TryAcquireAsync("discover-news", TimeSpan.FromMinutes(30));

        Assert.NotNull(first);
        Assert.Null(second);
    }

    [Fact]
    public async Task TryAcquireAsync_allows_new_holder_after_release()
    {
        await using (var lease = (await service.TryAcquireAsync("discover-news", TimeSpan.FromMinutes(30)))!)
        {
            Assert.NotNull(lease);
        }

        await using var next = (await service.TryAcquireAsync("discover-news", TimeSpan.FromMinutes(30)))!;

        Assert.NotNull(next);
    }

    [Fact]
    public async Task TryAcquireAsync_reclaims_expired_lease()
    {
        var expiredAt = DateTime.UtcNow.AddMinutes(-5);
        dbContext.NewsAgentRunLeases.Add(new NewsAgentRunLeaseEntity
        {
            LeaseName = "discover-news",
            HolderId = "expired-holder",
            AcquiredAtUtc = expiredAt.AddMinutes(-30),
            ExpiresAtUtc = expiredAt
        });
        await dbContext.SaveChangesAsync();

        await using var lease = (await service.TryAcquireAsync("discover-news", TimeSpan.FromMinutes(30)))!;

        Assert.NotNull(lease);
        Assert.NotEqual("expired-holder", lease.HolderId);
    }

    public async ValueTask DisposeAsync()
    {
        await dbContext.DisposeAsync();
        await connection.DisposeAsync();
    }
}
