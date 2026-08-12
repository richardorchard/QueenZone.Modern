using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QueenZone.Data;
using QueenZone.Data.Entities;

namespace QueenZone.Web.Tests;

public sealed class EfMemberAccountRepositoryTests : IAsyncDisposable
{
    private readonly SqliteConnection connection;
    private readonly QueenZoneDbContext dbContext;
    private readonly EfMemberAccountRepository repository;

    public EfMemberAccountRepositoryTests()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<QueenZoneDbContext>()
            .UseSqlite(connection)
            .Options;
        dbContext = new QueenZoneDbContext(options);
        dbContext.Database.EnsureCreated();
        repository = new EfMemberAccountRepository(dbContext);
    }

    [Fact]
    public async Task ListExternalProvidersAsync_ReturnsDistinctOrderedProviders()
    {
        var account = await SeedAccountAsync("ef-fan@example.com", "EF Fan");
        await repository.AddExternalLoginAsync(account.Id, "GitHub", "gh-1", account.Email);
        await repository.AddExternalLoginAsync(account.Id, "Google", "g-1", account.Email);
        await repository.AddExternalLoginAsync(account.Id, "Google", "g-2", account.Email);

        var providers = await repository.ListExternalProvidersAsync(account.Id);

        Assert.Equal(["GitHub", "Google"], providers);
    }

    [Fact]
    public async Task ListExternalProvidersAsync_ReturnsEmpty_WhenNoneLinked()
    {
        var account = await SeedAccountAsync("lonely@example.com", "Lonely");

        var providers = await repository.ListExternalProvidersAsync(account.Id);

        Assert.Empty(providers);
    }

    [Fact]
    public async Task UpdateDisplayNameAsync_PersistsNewName()
    {
        var account = await SeedAccountAsync("rename@example.com", "Before");

        var updated = await repository.UpdateDisplayNameAsync(account.Id, "After");

        Assert.NotNull(updated);
        Assert.Equal("After", updated.DisplayName);

        var reloaded = await repository.FindByIdAsync(account.Id);
        Assert.Equal("After", reloaded!.DisplayName);
    }

    [Fact]
    public async Task UpdateDisplayNameAsync_ReturnsNull_WhenAccountMissing()
    {
        var updated = await repository.UpdateDisplayNameAsync(Guid.NewGuid(), "Ghost");

        Assert.Null(updated);
    }

    [Fact]
    public async Task UpdateAvatarUrlAsync_PersistsAndClearsPath()
    {
        var account = await SeedAccountAsync("avatar-ef@example.com", "EF Avatar");

        var updated = await repository.UpdateAvatarUrlAsync(account.Id, "members/x/avatar.webp");
        Assert.Equal("members/x/avatar.webp", updated!.AvatarUrl);

        var cleared = await repository.UpdateAvatarUrlAsync(account.Id, null);
        Assert.Null(cleared!.AvatarUrl);
    }

    [Fact]
    public async Task LinkLegacyUserIdAsync_PersistsAndIsFindable()
    {
        var account = await SeedAccountAsync("legacy-link@example.com", "Legacy Link");

        var linked = await repository.LinkLegacyUserIdAsync(account.Id, 4242);
        Assert.Equal(4242, linked!.LinkedLegacyUserId);

        var found = await repository.FindByLinkedLegacyUserIdAsync(4242);
        Assert.NotNull(found);
        Assert.Equal(account.Id, found.Id);

        // Second link is a no-op when already set.
        var again = await repository.LinkLegacyUserIdAsync(account.Id, 9999);
        Assert.Equal(4242, again!.LinkedLegacyUserId);

        var unlinked = await repository.UnlinkLegacyUserIdAsync(account.Id);
        Assert.Null(unlinked!.LinkedLegacyUserId);
        Assert.Null(await repository.FindByLinkedLegacyUserIdAsync(4242));

        var relinked = await repository.LinkLegacyUserIdAsync(account.Id, 9999);
        Assert.Equal(9999, relinked!.LinkedLegacyUserId);
    }

    [Fact]
    public async Task SearchByDisplayNameAsync_MatchesAndExcludesMember()
    {
        var alice = await SeedAccountAsync("search-alice@example.com", "Search Alice");
        var bob = await SeedAccountAsync("search-bob@example.com", "Search Bob");
        await SeedAccountAsync("search-bobby@example.com", "Search Bobby");

        var matches = await repository.SearchByDisplayNameAsync("Search Bo", excludeMemberId: alice.Id);

        Assert.Contains(matches, m => m.MemberId == bob.Id);
        Assert.DoesNotContain(matches, m => m.MemberId == alice.Id);
        Assert.Contains(matches, m => m.DisplayName == "Search Bobby");
    }

    [Fact]
    public async Task SearchMembersAsync_MatchesDisplayNameOrEmail_AndPaginates()
    {
        await SeedAccountAsync("spammer@example.com", "Spam Bot");
        await SeedAccountAsync("regular@example.com", "Regular Fan");
        await SeedAccountAsync("other-spammer@junk.com", "Other Member");

        var byName = await repository.SearchMembersAsync("Spam", pageNumber: 1, pageSize: 50);
        Assert.Equal(1, byName.TotalCount);
        Assert.Equal("Spam Bot", byName.Members[0].DisplayName);

        var byEmail = await repository.SearchMembersAsync("junk.com", pageNumber: 1, pageSize: 50);
        Assert.Equal(1, byEmail.TotalCount);
        Assert.Equal("Other Member", byEmail.Members[0].DisplayName);

        var page1 = await repository.SearchMembersAsync(null, pageNumber: 1, pageSize: 2);
        Assert.Equal(3, page1.TotalCount);
        Assert.Equal(2, page1.Members.Count);

        var page2 = await repository.SearchMembersAsync(null, pageNumber: 2, pageSize: 2);
        Assert.Single(page2.Members);
    }

    [Fact]
    public async Task SuspendAsync_ThenReinstateAsync_RoundTrips()
    {
        var account = await SeedAccountAsync("spammer2@example.com", "Spam Bot 2");
        var suspendedAt = new DateTime(2026, 8, 12, 0, 0, 0, DateTimeKind.Utc);

        var suspended = await repository.SuspendAsync(account.Id, "Posting spam links", "admin@queenzone.org", suspendedAt);
        Assert.True(suspended!.IsSuspended);
        Assert.Equal("Posting spam links", suspended.SuspendedReason);
        Assert.Equal("admin@queenzone.org", suspended.SuspendedByAdminEmail);
        Assert.Equal(suspendedAt, suspended.SuspendedAt);

        var reinstated = await repository.ReinstateAsync(account.Id);
        Assert.False(reinstated!.IsSuspended);
        Assert.Null(reinstated.SuspendedReason);
        Assert.Null(reinstated.SuspendedByAdminEmail);
        Assert.Null(reinstated.SuspendedAt);
    }

    [Fact]
    public async Task SuspendAsync_ReturnsNull_WhenAccountMissing()
    {
        var result = await repository.SuspendAsync(Guid.NewGuid(), "reason", "admin@queenzone.org", DateTime.UtcNow);
        Assert.Null(result);
    }

    private async Task<MemberAccount> SeedAccountAsync(string email, string displayName)
    {
        return await repository.CreateAsync(new MemberAccount
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = displayName,
            CreatedAt = DateTime.UtcNow,
        });
    }

    public async ValueTask DisposeAsync()
    {
        await dbContext.DisposeAsync();
        await connection.DisposeAsync();
    }
}
