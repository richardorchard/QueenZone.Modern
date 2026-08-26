using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QueenZone.Data;
using QueenZone.Data.Entities;

namespace QueenZone.Web.Tests;

public sealed class EfNotificationPreferenceRepositoryTests : IAsyncDisposable
{
    private readonly SqliteConnection connection;
    private readonly QueenZoneDbContext dbContext;
    private readonly EfNotificationPreferenceRepository repository;

    public EfNotificationPreferenceRepositoryTests()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        dbContext = new QueenZoneDbContext(
            new DbContextOptionsBuilder<QueenZoneDbContext>().UseSqlite(connection).Options);
        dbContext.Database.EnsureCreated();
        repository = new EfNotificationPreferenceRepository(dbContext);
    }

    [Fact]
    public async Task ListEnabledAsync_News_ReturnsExplicitEnabledOnly()
    {
        var enabled = await SeedAccountAsync("news-on@example.com");
        var muted = await SeedAccountAsync("news-off@example.com");
        await SeedAccountAsync("news-default@example.com");
        await repository.ApplyAsync(enabled.Id, new NotificationPreferencePatch(null, null, true));
        await repository.ApplyAsync(muted.Id, new NotificationPreferencePatch(null, null, false));

        var listed = await repository.ListEnabledAsync(NotificationCategory.News);

        Assert.Equal([enabled.Id], listed);
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
