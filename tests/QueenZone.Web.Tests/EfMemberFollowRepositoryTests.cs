using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QueenZone.Data;
using QueenZone.Data.Entities;

namespace QueenZone.Web.Tests;

public sealed class EfMemberFollowRepositoryTests : IAsyncDisposable
{
    private readonly SqliteConnection connection;
    private readonly QueenZoneDbContext dbContext;
    private readonly EfMemberFollowRepository repository;
    private readonly Guid aliceId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private readonly Guid bobId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public EfMemberFollowRepositoryTests()
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
                Email = "alice-follow-ef@example.com",
                NormalizedEmail = "ALICE-FOLLOW-EF@EXAMPLE.COM",
                DisplayName = "Alice",
                CreatedAt = DateTime.UtcNow,
            },
            new MemberAccount
            {
                Id = bobId,
                Email = "bob-follow-ef@example.com",
                NormalizedEmail = "BOB-FOLLOW-EF@EXAMPLE.COM",
                DisplayName = "Bob",
                CreatedAt = DateTime.UtcNow,
            });
        dbContext.SaveChanges();
        repository = new EfMemberFollowRepository(dbContext);
    }

    [Fact]
    public async Task Follow_IsFollowing_Unfollow_RoundTrip()
    {
        Assert.False(await repository.IsFollowingAsync(aliceId, bobId));

        await repository.FollowAsync(aliceId, bobId, DateTimeOffset.Parse("2026-08-17T10:00:00Z"));
        Assert.True(await repository.IsFollowingAsync(aliceId, bobId));
        Assert.False(await repository.IsFollowingAsync(bobId, aliceId));

        await repository.FollowAsync(aliceId, bobId, DateTimeOffset.Parse("2026-08-17T10:01:00Z"));
        Assert.Equal(1, await dbContext.MemberFollows.CountAsync());

        Assert.True(await repository.UnfollowAsync(aliceId, bobId));
        Assert.False(await repository.IsFollowingAsync(aliceId, bobId));
        Assert.False(await repository.UnfollowAsync(aliceId, bobId));
    }

    [Fact]
    public async Task ListFollowedIdsAsync_ReturnsOnlyPeopleTheFollowerFollows()
    {
        var carolId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        dbContext.MemberAccounts.Add(new MemberAccount
        {
            Id = carolId,
            Email = "carol-follow-ef@example.com",
            NormalizedEmail = "CAROL-FOLLOW-EF@EXAMPLE.COM",
            DisplayName = "Carol",
            CreatedAt = DateTime.UtcNow,
        });
        await dbContext.SaveChangesAsync();

        await repository.FollowAsync(aliceId, bobId, DateTimeOffset.Parse("2026-08-17T10:00:00Z"));
        await repository.FollowAsync(aliceId, carolId, DateTimeOffset.Parse("2026-08-17T10:01:00Z"));
        await repository.FollowAsync(bobId, aliceId, DateTimeOffset.Parse("2026-08-17T10:02:00Z"));

        var aliceFollows = await repository.ListFollowedIdsAsync(aliceId);
        Assert.Equal([bobId, carolId], aliceFollows.OrderBy(id => id).ToArray());
        Assert.Equal([aliceId], await repository.ListFollowedIdsAsync(bobId));
        Assert.Empty(await repository.ListFollowedIdsAsync(carolId));
    }

    public async ValueTask DisposeAsync()
    {
        await dbContext.DisposeAsync();
        await connection.DisposeAsync();
    }
}
