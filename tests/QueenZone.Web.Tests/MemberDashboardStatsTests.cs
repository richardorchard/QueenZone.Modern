using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QueenZone.Data;
using QueenZone.Data.Entities;

namespace QueenZone.Web.Tests;

public sealed class MemberDashboardStatsTests : IAsyncDisposable
{
    private readonly SqliteConnection connection;
    private readonly QueenZoneDbContext dbContext;
    private readonly EfMemberAccountRepository repository;

    public MemberDashboardStatsTests()
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

    public async ValueTask DisposeAsync()
    {
        await dbContext.DisposeAsync();
        await connection.DisposeAsync();
    }

    private async Task<MemberAccount> SeedMemberAsync(string email, DateTime createdAt, DateTime? lastLoginAt = null)
    {
        var account = new MemberAccount
        {
            Id = Guid.NewGuid(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            DisplayName = email.Split('@')[0],
            CreatedAt = createdAt,
            LastLoginAt = lastLoginAt,
        };
        dbContext.MemberAccounts.Add(account);
        await dbContext.SaveChangesAsync();
        return account;
    }

    [Fact]
    public async Task GetStatsAsync_counts_members_correctly()
    {
        var now = new DateTime(2026, 7, 20, 12, 0, 0, DateTimeKind.Utc);
        await SeedMemberAsync("a@test.com", now.AddDays(-40));   // older than 30 days
        await SeedMemberAsync("b@test.com", now.AddDays(-10));   // within 30 days
        await SeedMemberAsync("c@test.com", now.AddDays(-3));    // within 7 days
        await SeedMemberAsync("d@test.com", now.AddHours(-1));   // today

        var stats = await repository.GetStatsAsync(now);

        Assert.Equal(4, stats.Total);
        Assert.Equal(1, stats.NewToday);
        Assert.Equal(2, stats.NewLast7Days);
        Assert.Equal(3, stats.NewLast30Days);
    }

    [Fact]
    public async Task GetStatsAsync_returns_zeros_when_no_members()
    {
        var stats = await repository.GetStatsAsync(DateTime.UtcNow);

        Assert.Equal(0, stats.Total);
        Assert.Equal(0, stats.NewToday);
        Assert.Equal(0, stats.NewLast7Days);
        Assert.Equal(0, stats.NewLast30Days);
    }

    [Fact]
    public async Task GetRecentLoginsAsync_returns_most_recent_logins_ordered_descending()
    {
        var now = DateTime.UtcNow;
        var a = await SeedMemberAsync("early@test.com", now.AddDays(-5), now.AddHours(-3));
        var b = await SeedMemberAsync("recent@test.com", now.AddDays(-5), now.AddMinutes(-10));
        await SeedMemberAsync("nologin@test.com", now.AddDays(-5)); // no LastLoginAt

        var logins = await repository.GetRecentLoginsAsync(5);

        Assert.Equal(2, logins.Count);
        Assert.Equal(b.Id, logins[0].MemberId);
        Assert.Equal(a.Id, logins[1].MemberId);
    }

    [Fact]
    public async Task GetRecentLoginsAsync_respects_count_limit()
    {
        var now = DateTime.UtcNow;
        for (var i = 0; i < 5; i++)
        {
            await SeedMemberAsync($"user{i}@test.com", now.AddDays(-10), now.AddHours(-i));
        }

        var logins = await repository.GetRecentLoginsAsync(3);

        Assert.Equal(3, logins.Count);
    }

    [Fact]
    public async Task GetRecentLoginsAsync_returns_empty_when_no_logins_recorded()
    {
        await SeedMemberAsync("nologin@test.com", DateTime.UtcNow.AddDays(-1));

        var logins = await repository.GetRecentLoginsAsync(5);

        Assert.Empty(logins);
    }

    [Fact]
    public async Task RecordLoginAsync_updates_LastLoginAt()
    {
        var created = await SeedMemberAsync("logintest@test.com", DateTime.UtcNow.AddDays(-1));
        var loginTime = new DateTime(2026, 7, 20, 9, 30, 0, DateTimeKind.Utc);

        await repository.RecordLoginAsync(created.Id, loginTime);

        var updated = await dbContext.MemberAccounts.FindAsync(created.Id);
        Assert.Equal(loginTime, updated!.LastLoginAt);
    }

    [Fact]
    public async Task GetDailyRegistrationsAsync_groups_by_day_correctly()
    {
        var fromDate = new DateOnly(2026, 7, 18);
        var baseTime = new DateTime(2026, 7, 18, 8, 0, 0, DateTimeKind.Utc);
        await SeedMemberAsync("d1a@test.com", baseTime);
        await SeedMemberAsync("d1b@test.com", baseTime.AddHours(5));
        await SeedMemberAsync("d2@test.com", baseTime.AddDays(1));

        var regs = await repository.GetDailyRegistrationsAsync(fromDate);

        Assert.Equal(2, regs.Count);
        Assert.Equal(2, regs.Single(r => r.Date == new DateOnly(2026, 7, 18)).Count);
        Assert.Equal(1, regs.Single(r => r.Date == new DateOnly(2026, 7, 19)).Count);
    }

    [Fact]
    public async Task GetDailyRegistrationsAsync_excludes_registrations_before_fromDate()
    {
        var fromDate = new DateOnly(2026, 7, 18);
        await SeedMemberAsync("old@test.com", new DateTime(2026, 7, 17, 23, 59, 0, DateTimeKind.Utc));
        await SeedMemberAsync("new@test.com", new DateTime(2026, 7, 18, 0, 1, 0, DateTimeKind.Utc));

        var regs = await repository.GetDailyRegistrationsAsync(fromDate);

        Assert.Single(regs);
        Assert.Equal(new DateOnly(2026, 7, 18), regs[0].Date);
    }

    [Fact]
    public async Task GetDailyRegistrationsAsync_returns_empty_when_no_registrations_in_range()
    {
        var regs = await repository.GetDailyRegistrationsAsync(new DateOnly(2026, 7, 18));

        Assert.Empty(regs);
    }
}
