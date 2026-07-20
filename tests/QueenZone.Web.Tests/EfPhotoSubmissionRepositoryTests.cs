using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QueenZone.Data;
using QueenZone.Data.Entities;

namespace QueenZone.Web.Tests;

public sealed class EfPhotoSubmissionRepositoryTests : IAsyncDisposable
{
    private readonly SqliteConnection connection;
    private readonly QueenZoneDbContext dbContext;
    private readonly EfPhotoSubmissionRepository repository;
    private readonly Guid memberId = Guid.NewGuid();

    public EfPhotoSubmissionRepositoryTests()
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
            Email = "photo-ef@example.com",
            NormalizedEmail = "PHOTO-EF@EXAMPLE.COM",
            DisplayName = "EF Photo Fan",
            CreatedAt = DateTime.UtcNow,
        });
        dbContext.SaveChanges();

        repository = new EfPhotoSubmissionRepository(dbContext);
    }

    [Fact]
    public async Task CreateAsync_PersistsPendingSubmissionAndAudit()
    {
        var preferredId = Guid.NewGuid();
        var created = await repository.CreateAsync(NewSubmission(preferredId, "Live shot", "Queen"));

        Assert.Equal(preferredId, created.Id);
        Assert.Equal(PhotoSubmissionStatus.Pending, created.Status);
        Assert.Equal("Live shot", created.Title);
        Assert.Equal("Queen", created.SuggestedCategory);

        var loaded = await repository.GetByIdAsync(created.Id);
        Assert.NotNull(loaded);
        Assert.Equal("EF Photo Fan", loaded!.SubmitterDisplayName);
        Assert.Equal("photo-ef@example.com", loaded.SubmitterEmail);

        Assert.Single(dbContext.PhotoSubmissionAuditLogs.Where(log => log.PhotoSubmissionId == created.Id));
    }

    [Fact]
    public async Task CreateAsync_GeneratesId_WhenPreferredMissing()
    {
        var created = await repository.CreateAsync(NewSubmission(null, "No preferred id", null));
        Assert.NotEqual(Guid.Empty, created.Id);
    }

    [Fact]
    public async Task GetPendingAsync_ReturnsReviewableStatusesNewestFirst()
    {
        var pending = await repository.CreateAsync(NewSubmission(Guid.NewGuid(), "Pending one", "A"));
        var reviewing = await repository.CreateAsync(NewSubmission(Guid.NewGuid(), "Reviewing", "B"));
        await repository.UpdateStatusAsync(
            reviewing.Id,
            PhotoSubmissionStatus.UnderReview,
            "admin@test.local",
            "looking",
            null);

        var needsInfo = await repository.CreateAsync(NewSubmission(Guid.NewGuid(), "Needs info", "C"));
        await repository.UpdateStatusAsync(
            needsInfo.Id,
            PhotoSubmissionStatus.NeedsInfo,
            "admin@test.local",
            "Need year",
            null);

        var approved = await repository.CreateAsync(NewSubmission(Guid.NewGuid(), "Approved", "D"));
        await repository.UpdateStatusAsync(
            approved.Id,
            PhotoSubmissionStatus.Approved,
            "admin@test.local",
            null,
            null,
            "Queen");

        var page = await repository.GetPendingAsync(1, 50);
        Assert.Equal(3, page.Count);
        Assert.DoesNotContain(page, item => item.Id == approved.Id);
        Assert.Contains(page, item => item.Id == pending.Id);
        Assert.All(page, item => Assert.Equal("EF Photo Fan", item.SubmitterDisplayName));
    }

    [Fact]
    public async Task GetPendingAsync_ClampsPageAndPageSize()
    {
        await repository.CreateAsync(NewSubmission(Guid.NewGuid(), "Only", null));
        var page = await repository.GetPendingAsync(0, 1000);
        Assert.Single(page);
    }

    [Fact]
    public async Task GetBySubmitterAsync_ReturnsOnlyOwnedRows()
    {
        var otherMember = Guid.NewGuid();
        dbContext.MemberAccounts.Add(new MemberAccount
        {
            Id = otherMember,
            Email = "other@example.com",
            NormalizedEmail = "OTHER@EXAMPLE.COM",
            DisplayName = "Other",
            CreatedAt = DateTime.UtcNow,
        });
        await dbContext.SaveChangesAsync();

        await repository.CreateAsync(NewSubmission(Guid.NewGuid(), "Mine", null));
        await repository.CreateAsync(NewSubmission(Guid.NewGuid(), "Theirs", null) with
        {
            SubmitterMemberId = otherMember,
        });

        var mine = await repository.GetBySubmitterAsync(memberId);
        Assert.Single(mine.Items);
        Assert.Equal("Mine", mine.Items[0].Title);
        Assert.Equal(1, mine.TotalCount);
    }

    [Fact]
    public async Task GetBySubmitterAsync_PaginatesNewestFirst()
    {
        for (var i = 0; i < 3; i++)
        {
            await repository.CreateAsync(NewSubmission(Guid.NewGuid(), $"Shot {i}", null));
        }

        var page = await repository.GetBySubmitterAsync(memberId, page: 1, pageSize: 2);
        Assert.Equal(3, page.TotalCount);
        Assert.Equal(2, page.Items.Count);
        Assert.Equal("Shot 2", page.Items[0].Title);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenMissing()
    {
        Assert.Null(await repository.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task UpdateStatusAsync_ApproveRejectNeedsInfoAndUnknown()
    {
        var forApprove = await repository.CreateAsync(NewSubmission(Guid.NewGuid(), "Approve me", "Brian May"));
        var approved = await repository.UpdateStatusAsync(
            forApprove.Id,
            PhotoSubmissionStatus.Approved,
            "admin@test.local",
            "  Nice  ",
            null,
            "  Brian May  ");
        Assert.Equal(PhotoSubmissionStatus.Approved, approved!.Status);
        Assert.Equal("Brian May", approved.ApprovedCategory);
        Assert.Equal("Nice", approved.ReviewNotes);

        var forReject = await repository.CreateAsync(NewSubmission(Guid.NewGuid(), "Reject me", null));
        var rejected = await repository.UpdateStatusAsync(
            forReject.Id,
            PhotoSubmissionStatus.Rejected,
            "admin@test.local",
            "internal",
            "  Blurry  ");
        Assert.Equal(PhotoSubmissionStatus.Rejected, rejected!.Status);
        Assert.Equal("Blurry", rejected.RejectionReason);

        var forNeedsInfo = await repository.CreateAsync(NewSubmission(Guid.NewGuid(), "Info", "Queen"));
        var needsInfo = await repository.UpdateStatusAsync(
            forNeedsInfo.Id,
            PhotoSubmissionStatus.NeedsInfo,
            "admin@test.local",
            "Please add year",
            null);
        Assert.Equal(PhotoSubmissionStatus.NeedsInfo, needsInfo!.Status);

        var noCategory = await repository.CreateAsync(NewSubmission(Guid.NewGuid(), "No category", null));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.UpdateStatusAsync(
                noCategory.Id,
                PhotoSubmissionStatus.Approved,
                "admin@test.local",
                null,
                null,
                approvedCategory: null));

        var missing = await repository.UpdateStatusAsync(
            Guid.NewGuid(),
            PhotoSubmissionStatus.UnderReview,
            "admin@test.local",
            null,
            null);
        Assert.Null(missing);
    }

    [Fact]
    public async Task UpdateStatusAsync_RejectWithoutReason_Throws()
    {
        var created = await repository.CreateAsync(NewSubmission(Guid.NewGuid(), "No reason", null));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.UpdateStatusAsync(
                created.Id,
                PhotoSubmissionStatus.Rejected,
                "admin@test.local",
                null,
                rejectionReason: "   "));
    }

    [Fact]
    public async Task UpdateStatusAsync_ApproveFallsBackToSuggestedCategory()
    {
        var created = await repository.CreateAsync(NewSubmission(Guid.NewGuid(), "Fallback", "Queen"));
        var approved = await repository.UpdateStatusAsync(
            created.Id,
            PhotoSubmissionStatus.Approved,
            "admin@test.local",
            null,
            null,
            approvedCategory: null);
        Assert.Equal("Queen", approved!.ApprovedCategory);
    }

    [Fact]
    public async Task UpdateStatusAsync_TruncatesLongOptionalFields()
    {
        var created = await repository.CreateAsync(
            NewSubmission(Guid.NewGuid(), "Truncate", null) with
            {
                Description = new string('d', 1200),
                SuggestedCategory = new string('c', 150),
            });

        Assert.Equal(1000, created.Description!.Length);
        Assert.Equal(100, created.SuggestedCategory!.Length);

        var underReview = await repository.UpdateStatusAsync(
            created.Id,
            PhotoSubmissionStatus.UnderReview,
            new string('e', 300),
            new string('n', 600),
            rejectionReason: new string('r', 600),
            approvedCategory: new string('a', 150));

        Assert.Equal(256, underReview!.ReviewerEmail!.Length);
        Assert.Equal(500, underReview.ReviewNotes!.Length);
        Assert.Equal(100, underReview.ApprovedCategory!.Length);
        Assert.Equal(500, underReview.RejectionReason!.Length);
    }

    private NewPhotoSubmission NewSubmission(Guid? id, string title, string? category) =>
        new(
            memberId,
            title,
            "desc",
            category,
            1986,
            new DateOnly(1986, 7, 12),
            "members/x/original.jpg",
            "members/x/display.webp",
            "members/x/thumb.webp",
            "photo.jpg",
            2048,
            "image/jpeg",
            800,
            600,
            id);

    public async ValueTask DisposeAsync()
    {
        await dbContext.DisposeAsync();
        await connection.DisposeAsync();
    }
}
