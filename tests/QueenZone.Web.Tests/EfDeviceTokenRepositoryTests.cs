using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QueenZone.Data;
using QueenZone.Data.Entities;

namespace QueenZone.Web.Tests;

public sealed class EfDeviceTokenRepositoryTests : IAsyncDisposable
{
    private readonly SqliteConnection connection;
    private readonly QueenZoneDbContext dbContext;
    private readonly EfDeviceTokenRepository repository;

    public EfDeviceTokenRepositoryTests()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        dbContext = new QueenZoneDbContext(
            new DbContextOptionsBuilder<QueenZoneDbContext>().UseSqlite(connection).Options);
        dbContext.Database.EnsureCreated();
        repository = new EfDeviceTokenRepository(dbContext);
    }

    [Fact]
    public async Task ListByMemberIdsAsync_ReturnsOwnedTokens()
    {
        var alice = await SeedAccountAsync("alice-token@example.com");
        var bob = await SeedAccountAsync("bob-token@example.com");
        await repository.UpsertAsync(DeviceTokenTestData.Token(alice.Id, DevicePushPlatform.Apns, "alice-a"));
        await repository.UpsertAsync(DeviceTokenTestData.Token(alice.Id, DevicePushPlatform.Fcm, "alice-f"));
        await repository.UpsertAsync(DeviceTokenTestData.Token(bob.Id, DevicePushPlatform.Apns, "bob-a"));

        var listed = await repository.ListByMemberIdsAsync([alice.Id]);

        Assert.Equal(2, listed.Count);
        Assert.All(listed, row => Assert.Equal(alice.Id, row.MemberAccountId));
    }

    [Fact]
    public async Task ListByMemberIdsAsync_EmptyInput_ReturnsEmpty()
    {
        Assert.Empty(await repository.ListByMemberIdsAsync([]));
    }

    private async Task<MemberAccount> SeedAccountAsync(string email)
    {
        var account = new MemberAccount
        {
            Id = Guid.NewGuid(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            DisplayName = email,
            CreatedAt = DateTime.UtcNow,
        };
        dbContext.MemberAccounts.Add(account);
        await dbContext.SaveChangesAsync();
        return account;
    }

    public async ValueTask DisposeAsync()
    {
        await dbContext.DisposeAsync();
        await connection.DisposeAsync();
    }
}
