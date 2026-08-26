using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QueenZone.Data;
using QueenZone.Data.Entities;

namespace QueenZone.Web.Tests;

public sealed class EfTopicWatchRepositoryTests : IAsyncDisposable
{
    private readonly SqliteConnection connection;
    private readonly QueenZoneDbContext dbContext;
    private readonly EfTopicWatchRepository repository;
    private readonly Guid aliceId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private readonly Guid bobId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public EfTopicWatchRepositoryTests()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<QueenZoneDbContext>()
            .UseSqlite(connection)
            .Options;
        dbContext = new QueenZoneDbContext(options);
        dbContext.Database.EnsureCreated();
        dbContext.MemberAccounts.AddRange(
            new MemberAccount
            {
                Id = aliceId,
                Email = "alice-watch-ef@example.com",
                NormalizedEmail = "ALICE-WATCH-EF@EXAMPLE.COM",
                DisplayName = "Alice",
                CreatedAt = DateTime.UtcNow,
            },
            new MemberAccount
            {
                Id = bobId,
                Email = "bob-watch-ef@example.com",
                NormalizedEmail = "BOB-WATCH-EF@EXAMPLE.COM",
                DisplayName = "Bob",
                CreatedAt = DateTime.UtcNow,
            });
        dbContext.SaveChanges();
        repository = new EfTopicWatchRepository(dbContext);
    }

    [Fact]
    public async Task Watch_List_Unwatch_RoundTrip_IsIdempotentAndUnique()
    {
        Assert.False(await repository.IsWatchingAsync(aliceId, 1002));
        Assert.Empty(await repository.ListMemberIdsAsync(1002));

        await repository.WatchAsync(aliceId, 1002, DateTimeOffset.Parse("2026-08-26T10:00:00Z"));
        await repository.WatchAsync(aliceId, 1002, DateTimeOffset.Parse("2026-08-26T10:01:00Z"));
        await repository.WatchAsync(bobId, 1002, DateTimeOffset.Parse("2026-08-26T10:02:00Z"));
        await repository.WatchAsync(aliceId, 1003, DateTimeOffset.Parse("2026-08-26T10:03:00Z"));

        Assert.True(await repository.IsWatchingAsync(aliceId, 1002));
        Assert.True(await repository.IsWatchingAsync(bobId, 1002));
        Assert.False(await repository.IsWatchingAsync(bobId, 1003));
        Assert.Equal(2, await dbContext.MemberTopicWatches.CountAsync(watch => watch.TopicId == 1002));

        var watchers = await repository.ListMemberIdsAsync(1002);
        Assert.Equal(2, watchers.Count);
        Assert.Contains(aliceId, watchers);
        Assert.Contains(bobId, watchers);

        Assert.True(await repository.UnwatchAsync(aliceId, 1002));
        Assert.False(await repository.IsWatchingAsync(aliceId, 1002));
        Assert.False(await repository.UnwatchAsync(aliceId, 1002));
        Assert.Equal(bobId, Assert.Single(await repository.ListMemberIdsAsync(1002)));
    }

    public async ValueTask DisposeAsync()
    {
        await dbContext.DisposeAsync();
        await connection.DisposeAsync();
    }
}
