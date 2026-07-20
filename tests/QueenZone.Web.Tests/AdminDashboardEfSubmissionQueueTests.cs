using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QueenZone.Data;
using QueenZone.Data.Entities;

namespace QueenZone.Web.Tests;

/// <summary>
/// EF / SQLite-backed tests for the dashboard count methods added in issue #291.
/// </summary>
public sealed class AdminDashboardEfSubmissionQueueTests : IAsyncLifetime
{
    private readonly SqliteConnection connection;
    private QueenZoneDbContext dbContext = null!;

    public AdminDashboardEfSubmissionQueueTests()
    {
        connection = new SqliteConnection("Data Source=:memory:");
    }

    public async Task InitializeAsync()
    {
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<QueenZoneDbContext>()
            .UseSqlite(connection)
            .Options;
        dbContext = new QueenZoneDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await dbContext.DisposeAsync();
        await connection.DisposeAsync();
    }

    // ── Photo submissions ───────────────────────────────────────────────────

    [Fact]
    public async Task EfPhoto_GetDashboardCounts_ReturnsZerosWhenEmpty()
    {
        var repo = new EfPhotoSubmissionRepository(dbContext);
        var counts = await repo.GetDashboardCountsAsync(DateTimeOffset.UtcNow);

        Assert.Equal(SubmissionTypeCounts.Empty, counts);
    }

    [Fact]
    public async Task EfPhoto_GetDashboardCounts_CountsPendingAndRecent()
    {
        var member = await SeedMemberAsync("photo-user@example.com", "Photo User");
        var repo = new EfPhotoSubmissionRepository(dbContext);
        var utcNow = DateTimeOffset.UtcNow;

        await repo.CreateAsync(SamplePhoto(member.Id), default);
        await repo.CreateAsync(SamplePhoto(member.Id), default);

        var counts = await repo.GetDashboardCountsAsync(utcNow);

        Assert.Equal(2, counts.Pending);
        Assert.Equal(2, counts.ReceivedToday);
        Assert.Equal(2, counts.ReceivedThisWeek);
    }

    [Fact]
    public async Task EfPhoto_GetDashboardCounts_ApprovalRateInLast30Days()
    {
        var member = await SeedMemberAsync("photo-rate@example.com", "Rate User");
        var repo = new EfPhotoSubmissionRepository(dbContext);
        var utcNow = DateTimeOffset.UtcNow;

        var sub = await repo.CreateAsync(SamplePhoto(member.Id), default);
        await repo.UpdateStatusAsync(sub.Id, PhotoSubmissionStatus.Approved, "admin@test.local", null, null,
            approvedCategory: "Queen", default);

        var counts = await repo.GetDashboardCountsAsync(utcNow);

        Assert.Equal(1, counts.ApprovedLast30Days);
        Assert.Equal(0, counts.RejectedLast30Days);
        Assert.Equal(0, counts.StillPendingFromLast30Days);
    }

    [Fact]
    public async Task EfPhoto_GetTopContributors_GroupsByMember()
    {
        var member = await SeedMemberAsync("photo-contrib@example.com", "Photo Contrib");
        var repo = new EfPhotoSubmissionRepository(dbContext);
        var monthStart = new DateTimeOffset(DateTimeOffset.UtcNow.Year, DateTimeOffset.UtcNow.Month, 1, 0, 0, 0, TimeSpan.Zero);

        await repo.CreateAsync(SamplePhoto(member.Id), default);
        await repo.CreateAsync(SamplePhoto(member.Id), default);

        var contributors = await repo.GetTopContributorsThisMonthAsync(monthStart, 5);

        Assert.Single(contributors);
        Assert.Equal("Photo Contrib", contributors[0].DisplayName);
        Assert.Equal(2, contributors[0].Count);
    }

    [Fact]
    public async Task EfPhoto_GetTopContributors_ReturnsEmpty_WhenNoSubmissionsThisMonth()
    {
        var repo = new EfPhotoSubmissionRepository(dbContext);
        var futureMonthStart = DateTimeOffset.UtcNow.AddMonths(1);

        var contributors = await repo.GetTopContributorsThisMonthAsync(futureMonthStart, 5);

        Assert.Empty(contributors);
    }

    // ── News suggestions ────────────────────────────────────────────────────

    [Fact]
    public async Task EfNews_GetDashboardCounts_ReturnsZerosWhenEmpty()
    {
        var repo = new EfNewsSuggestionRepository(dbContext);
        var counts = await repo.GetDashboardCountsAsync(DateTimeOffset.UtcNow);

        Assert.Equal(SubmissionTypeCounts.Empty, counts);
    }

    [Fact]
    public async Task EfNews_GetDashboardCounts_CountsPendingAndRecent()
    {
        var member = await SeedMemberAsync("news-user@example.com", "News User");
        var repo = new EfNewsSuggestionRepository(dbContext);
        var utcNow = DateTimeOffset.UtcNow;

        await repo.CreateAsync(SampleNewsSuggestion(member.Id, "https://example.com/a"), default);
        await repo.CreateAsync(SampleNewsSuggestion(member.Id, "https://example.com/b"), default);

        var counts = await repo.GetDashboardCountsAsync(utcNow);

        Assert.Equal(2, counts.Pending);
        Assert.Equal(2, counts.ReceivedToday);
    }

    [Fact]
    public async Task EfNews_GetTopContributors_GroupsByMember()
    {
        var member = await SeedMemberAsync("news-contrib@example.com", "News Contrib");
        var repo = new EfNewsSuggestionRepository(dbContext);
        var monthStart = new DateTimeOffset(DateTimeOffset.UtcNow.Year, DateTimeOffset.UtcNow.Month, 1, 0, 0, 0, TimeSpan.Zero);

        await repo.CreateAsync(SampleNewsSuggestion(member.Id, "https://example.com/x"), default);

        var contributors = await repo.GetTopContributorsThisMonthAsync(monthStart, 5);

        Assert.Single(contributors);
        Assert.Equal("News Contrib", contributors[0].DisplayName);
        Assert.Equal(1, contributors[0].Count);
    }

    // ── Article submissions ─────────────────────────────────────────────────

    [Fact]
    public async Task EfArticle_GetDashboardCounts_ReturnsZerosWhenEmpty()
    {
        var repo = new EfArticleSubmissionRepository(dbContext);
        var counts = await repo.GetDashboardCountsAsync(DateTimeOffset.UtcNow);

        Assert.Equal(SubmissionTypeCounts.Empty, counts);
    }

    [Fact]
    public async Task EfArticle_GetDashboardCounts_CountsSubmittedAsPending()
    {
        var member = await SeedMemberAsync("article-user@example.com", "Article User");
        var repo = new EfArticleSubmissionRepository(dbContext);
        var utcNow = DateTimeOffset.UtcNow;

        var draft = await repo.UpsertDraftAsync(SampleArticleDraft(member.Id), default);
        await repo.SubmitForReviewAsync(draft.Id, member.Id, default);

        var counts = await repo.GetDashboardCountsAsync(utcNow);

        Assert.Equal(1, counts.Pending);
        Assert.Equal(1, counts.ReceivedToday);
    }

    [Fact]
    public async Task EfArticle_GetTopContributors_GroupsByMember()
    {
        var member = await SeedMemberAsync("article-contrib@example.com", "Article Contrib");
        var repo = new EfArticleSubmissionRepository(dbContext);
        var monthStart = new DateTimeOffset(DateTimeOffset.UtcNow.Year, DateTimeOffset.UtcNow.Month, 1, 0, 0, 0, TimeSpan.Zero);

        var draft = await repo.UpsertDraftAsync(SampleArticleDraft(member.Id, "Title A"), default);
        await repo.SubmitForReviewAsync(draft.Id, member.Id, default);

        var contributors = await repo.GetTopContributorsThisMonthAsync(monthStart, 5);

        Assert.Single(contributors);
        Assert.Equal("Article Contrib", contributors[0].DisplayName);
        Assert.Equal(1, contributors[0].Count);
    }

    [Fact]
    public async Task EfArticle_GetTopContributors_ReturnsEmpty_ForDraftsNotSubmitted()
    {
        var member = await SeedMemberAsync("draft-only@example.com", "Draft User");
        var repo = new EfArticleSubmissionRepository(dbContext);
        var monthStart = new DateTimeOffset(DateTimeOffset.UtcNow.Year, DateTimeOffset.UtcNow.Month, 1, 0, 0, 0, TimeSpan.Zero);

        await repo.UpsertDraftAsync(SampleArticleDraft(member.Id), default);

        var contributors = await repo.GetTopContributorsThisMonthAsync(monthStart, 5);

        Assert.Empty(contributors);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private async Task<MemberAccount> SeedMemberAsync(string email, string displayName)
    {
        var entity = new MemberAccount
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = displayName,
        };
        dbContext.MemberAccounts.Add(entity);
        await dbContext.SaveChangesAsync();
        return entity;
    }

    private static NewPhotoSubmission SamplePhoto(Guid memberId) =>
        new(
            Id: Guid.NewGuid(),
            SubmitterMemberId: memberId,
            Title: "Test Photo",
            Description: null,
            SuggestedCategory: "Queen",
            ApproximateYear: null,
            ApproximateDate: null,
            BlobPath: "originals/test.jpg",
            WebOptimizedBlobPath: "web/test.webp",
            ThumbnailBlobPath: "thumbs/test.webp",
            OriginalFileName: "test.jpg",
            FileSizeBytes: 1024,
            MimeType: "image/jpeg",
            ImageWidthPx: 800,
            ImageHeightPx: 600);

    private static NewsSuggestion SampleNewsSuggestion(Guid memberId, string url) =>
        new(
            Id: Guid.Empty,
            SubmitterMemberId: memberId,
            Url: url,
            UrlHash: Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(url))),
            Title: "Sample News",
            Notes: null,
            Status: NewsSuggestionStatus.Pending,
            SubmittedAt: DateTimeOffset.UtcNow,
            ReviewedAt: null,
            ReviewerEmail: null,
            ReviewNotes: null,
            PromotedNewsId: null,
            DuplicateCandidateId: null,
            SubmitterDisplayName: null,
            SubmitterEmail: null);

    private static ArticleSubmissionDraft SampleArticleDraft(Guid memberId, string title = "Test Article") =>
        new(
            Id: null,
            AuthorMemberId: memberId,
            Title: title,
            Excerpt: "A test excerpt.",
            Body: new string('x', 350),
            CoverImageBlobPath: null,
            Tags: null);
}
