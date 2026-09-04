using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QueenZone.Data;
using QueenZone.Data.Entities;

namespace QueenZone.Web.Tests;

public sealed class EfFanPerformanceSubmissionRepositoryTests : IAsyncDisposable
{
    private readonly SqliteConnection connection;
    private readonly QueenZoneDbContext dbContext;
    private readonly EfFanPerformanceSubmissionRepository repository;
    private readonly Guid memberId = Guid.NewGuid();

    public EfFanPerformanceSubmissionRepositoryTests()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<QueenZoneDbContext>()
            .UseSqlite(connection)
            .Options;
        dbContext = new QueenZoneDbContext(options);
        dbContext.Database.EnsureCreated();

        dbContext.MemberAccounts.Add(new MemberAccount
        {
            Id = memberId,
            Email = "fanperf-ef@example.com",
            NormalizedEmail = "FANPERF-EF@EXAMPLE.COM",
            DisplayName = "EF Stage Fan",
            CreatedAt = DateTime.UtcNow,
        });
        dbContext.SaveChanges();

        repository = new EfFanPerformanceSubmissionRepository(dbContext);
    }

    [Fact]
    public async Task CreateAsync_PersistsPendingSubmissionAndAudit()
    {
        var preferredId = Guid.NewGuid();
        var created = await repository.CreateAsync(NewSubmission(preferredId, "Live cover"));

        Assert.Equal(preferredId, created.Id);
        Assert.Equal(FanPerformanceSubmissionStatus.Pending, created.Status);
        Assert.Equal("Live cover", created.Title);
        Assert.Equal("Bohemian Rhapsody", created.CoveredSong);
        Assert.Equal(FanPerformanceSubmissionRights.DeclarationVersion, created.RightsDeclarationVersion);
        Assert.Null(created.PromotedStageId);

        var loaded = await repository.GetByIdAsync(created.Id);
        Assert.NotNull(loaded);
        Assert.Equal("EF Stage Fan", loaded!.SubmitterDisplayName);
        Assert.Equal("fanperf-ef@example.com", loaded.SubmitterEmail);

        Assert.Single(dbContext.FanPerformanceSubmissionAuditLogs.Where(log => log.FanPerformanceSubmissionId == created.Id));
    }

    [Fact]
    public async Task CreateAsync_GeneratesId_WhenPreferredMissing()
    {
        var created = await repository.CreateAsync(NewSubmission(null, "No preferred id"));
        Assert.NotEqual(Guid.Empty, created.Id);
    }

    [Fact]
    public async Task GetBySubmitterAsync_ReturnsOnlyOwnedRows()
    {
        var otherMember = Guid.NewGuid();
        dbContext.MemberAccounts.Add(new MemberAccount
        {
            Id = otherMember,
            Email = "other-fanperf@example.com",
            NormalizedEmail = "OTHER-FANPERF@EXAMPLE.COM",
            DisplayName = "Other",
            CreatedAt = DateTime.UtcNow,
        });
        await dbContext.SaveChangesAsync();

        await repository.CreateAsync(NewSubmission(Guid.NewGuid(), "Mine"));
        await repository.CreateAsync(NewSubmission(Guid.NewGuid(), "Theirs") with
        {
            SubmitterMemberId = otherMember,
        });

        var mine = await repository.GetBySubmitterAsync(memberId);
        Assert.Single(mine.Items);
        Assert.Equal("Mine", mine.Items[0].Title);
        Assert.Equal(1, mine.TotalCount);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenMissing()
    {
        Assert.Null(await repository.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task UpdateStatusAsync_WithdrawAndNeedsInfoReply()
    {
        var forWithdraw = await repository.CreateAsync(NewSubmission(Guid.NewGuid(), "Withdraw me"));
        var withdrawn = await repository.UpdateStatusAsync(
            forWithdraw.Id,
            FanPerformanceSubmissionStatus.Withdrawn,
            string.Empty,
            null,
            null,
            "Member withdrew the submission.");
        Assert.Equal(FanPerformanceSubmissionStatus.Withdrawn, withdrawn!.Status);

        var forReply = await repository.CreateAsync(NewSubmission(Guid.NewGuid(), "Reply me"));
        var needsInfo = await repository.UpdateStatusAsync(
            forReply.Id,
            FanPerformanceSubmissionStatus.NeedsInfo,
            "admin@test.local",
            "Please add the song",
            null);
        Assert.Equal(FanPerformanceSubmissionStatus.NeedsInfo, needsInfo!.Status);

        var replied = await repository.UpdateStatusAsync(
            forReply.Id,
            FanPerformanceSubmissionStatus.UnderReview,
            string.Empty,
            null,
            null,
            "It is Reaching Out.");
        Assert.Equal(FanPerformanceSubmissionStatus.UnderReview, replied!.Status);
        Assert.Equal("Please add the song", replied.ReviewNotes);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.UpdateStatusAsync(
                forWithdraw.Id,
                FanPerformanceSubmissionStatus.Pending,
                "admin@test.local",
                null,
                null));

        Assert.Null(await repository.UpdateStatusAsync(
            Guid.NewGuid(),
            FanPerformanceSubmissionStatus.UnderReview,
            "admin@test.local",
            null,
            null));
    }

    [Fact]
    public async Task UpdateStatusAsync_RejectRequiresReason()
    {
        var created = await repository.CreateAsync(NewSubmission(Guid.NewGuid(), "Reject me"));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.UpdateStatusAsync(
                created.Id,
                FanPerformanceSubmissionStatus.Rejected,
                "admin@test.local",
                null,
                rejectionReason: null));

        var rejected = await repository.UpdateStatusAsync(
            created.Id,
            FanPerformanceSubmissionStatus.Rejected,
            "admin@test.local",
            "internal",
            "  Not a Queen cover  ");
        Assert.Equal(FanPerformanceSubmissionStatus.Rejected, rejected!.Status);
        Assert.Equal("Not a Queen cover", rejected.RejectionReason);
    }

    public ValueTask DisposeAsync()
    {
        dbContext.Dispose();
        connection.Dispose();
        return ValueTask.CompletedTask;
    }

    private NewFanPerformanceSubmission NewSubmission(Guid? id, string title) =>
        new(
            memberId,
            title,
            "Bohemian Rhapsody",
            "EF Stage Fan",
            "Studio take",
            $"members/{memberId:N}/cover.mp3",
            "cover.mp3",
            2048,
            "audio/mpeg",
            120,
            DateTimeOffset.UtcNow,
            FanPerformanceSubmissionRights.DeclarationVersion,
            id);
}
