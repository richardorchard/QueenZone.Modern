using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QueenZone.Data;
using QueenZone.Data.Entities;

namespace QueenZone.Web.Tests;

public sealed class EfMemberPublicActivityRepositoryTests : IAsyncDisposable
{
    private readonly SqliteConnection connection;
    private readonly QueenZoneDbContext dbContext;
    private readonly Guid memberId = Guid.NewGuid();

    public EfMemberPublicActivityRepositoryTests()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        dbContext = new QueenZoneDbContext(new DbContextOptionsBuilder<QueenZoneDbContext>()
            .UseSqlite(connection)
            .Options);
        dbContext.Database.EnsureCreated();
        dbContext.Database.ExecuteSqlRaw("""
            CREATE TABLE ModernForumThread (
                Id INTEGER PRIMARY KEY,
                LegacyTopicId INTEGER NOT NULL,
                LegacyForumId INTEGER NOT NULL,
                CategoryId INTEGER NOT NULL,
                Title TEXT NOT NULL,
                StartedByLegacyUserId INTEGER NULL,
                StartedByDisplayName TEXT NOT NULL,
                StartedAt TEXT NULL,
                LastActivityAt TEXT NULL,
                ReplyCount INTEGER NOT NULL,
                IsSticky INTEGER NOT NULL,
                IsLegacyTopicStarter INTEGER NOT NULL,
                LegacyDiscography INTEGER NOT NULL,
                StartedByUserValidated INTEGER NULL,
                StarterAttachment TEXT NULL,
                StarterFileSize TEXT NULL,
                StarterAttachCount INTEGER NOT NULL,
                ImportedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );
            """);
        dbContext.Database.ExecuteSqlRaw("""
            CREATE TABLE ModernForumPost (
                Id INTEGER PRIMARY KEY,
                LegacyPostId INTEGER NOT NULL,
                LegacyThreadTopicId INTEGER NOT NULL,
                ThreadId INTEGER NOT NULL,
                LegacyForumId INTEGER NOT NULL,
                AuthorLegacyUserId INTEGER NULL,
                AuthorMemberId TEXT NULL,
                AuthorDisplayName TEXT NOT NULL,
                AuthorPostCount INTEGER NULL,
                AuthorJoinedAt TEXT NULL,
                BodyHtml TEXT NOT NULL,
                BodyHtmlLegacyRaw TEXT NULL,
                SignatureHtml TEXT NULL,
                PostedAt TEXT NULL,
                EditedAt TEXT NULL,
                EditCount INTEGER NOT NULL DEFAULT 0,
                LegacyDiscography INTEGER NOT NULL,
                AuthorUserValidated INTEGER NULL,
                Attachment TEXT NULL,
                FileSize TEXT NULL,
                AttachCount INTEGER NOT NULL,
                ImportedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                FOREIGN KEY (ThreadId) REFERENCES ModernForumThread(Id)
            );
            """);
    }

    [Fact]
    public async Task GetPageAsync_ReturnsOnlyPublicMemberContributions()
    {
        var now = DateTimeOffset.Parse("2026-08-03T08:00:00Z");
        dbContext.MemberAccounts.Add(new MemberAccount
        {
            Id = memberId,
            Email = "public-activity@example.com",
            DisplayName = "Public Activity",
            CreatedAt = now.UtcDateTime,
        });
        dbContext.ModernForumThreads.Add(new ModernForumThreadEntity
        {
            Id = 1,
            LegacyTopicId = 101,
            LegacyForumId = 1,
            CategoryId = 1,
            Title = "Public topic",
        });
        dbContext.ModernForumPosts.Add(new ModernForumPostEntity
        {
            Id = 1,
            LegacyPostId = 201,
            LegacyThreadTopicId = 101,
            ThreadId = 1,
            LegacyForumId = 1,
            AuthorMemberId = memberId,
            AuthorDisplayName = "Public Activity",
            BodyHtml = "Public forum post",
            PostedAt = now.UtcDateTime,
        });
        dbContext.ArticleSubmissions.AddRange(
            Article("Published article", ArticleSubmissionStatus.Published, now.AddHours(-1)),
            Article("Private draft", ArticleSubmissionStatus.Draft, null));
        dbContext.PhotoSubmissions.AddRange(
            Photo("Approved photo", PhotoSubmissionStatus.Approved, now.AddHours(-2)),
            Photo("Pending photo", PhotoSubmissionStatus.Pending, null));
        dbContext.NewsSuggestions.AddRange(
            News("Promoted news", NewsSuggestionStatus.Promoted, 301, now.AddHours(-3)),
            News("Pending news", NewsSuggestionStatus.Pending, null, null));
        await dbContext.SaveChangesAsync();

        var result = await new EfMemberPublicActivityRepository(dbContext)
            .GetPageAsync(memberId, 1, 20);

        Assert.Equal(4, result.TotalCount);
        Assert.Equal(
            [MemberPublicActivityType.ForumPost, MemberPublicActivityType.Article, MemberPublicActivityType.Photo, MemberPublicActivityType.News],
            result.Items.Select(item => item.Type).ToArray());
        Assert.DoesNotContain(result.Items, item => item.Title.Contains("Private", StringComparison.Ordinal));
        Assert.DoesNotContain(result.Items, item => item.Title.Contains("Pending", StringComparison.Ordinal));
    }

    private ArticleSubmissionEntity Article(string title, string status, DateTimeOffset? publishedAt) => new()
    {
        Id = Guid.NewGuid(),
        AuthorMemberId = memberId,
        Title = title,
        Slug = NewsSlug.Slugify(title),
        Body = "Body",
        Status = status,
        PublishedAt = publishedAt,
    };

    private PhotoSubmissionEntity Photo(string title, string status, DateTimeOffset? reviewedAt) => new()
    {
        Id = Guid.NewGuid(),
        SubmitterMemberId = memberId,
        Title = title,
        BlobPath = $"original/{Guid.NewGuid():N}.jpg",
        WebOptimizedBlobPath = $"web/{Guid.NewGuid():N}.webp",
        ThumbnailBlobPath = $"thumb/{Guid.NewGuid():N}.webp",
        OriginalFileName = "photo.jpg",
        MimeType = "image/jpeg",
        Status = status,
        SubmittedAt = DateTimeOffset.Parse("2026-08-01T08:00:00Z"),
        ReviewedAt = reviewedAt,
    };

    private NewsSuggestionEntity News(string title, string status, int? newsId, DateTimeOffset? reviewedAt) => new()
    {
        Id = Guid.NewGuid(),
        SubmitterMemberId = memberId,
        Url = $"https://example.com/{Guid.NewGuid():N}",
        UrlHash = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
        Title = title,
        Status = status,
        SubmittedAt = DateTimeOffset.Parse("2026-08-01T08:00:00Z"),
        ReviewedAt = reviewedAt,
        PromotedNewsId = newsId,
    };

    public async ValueTask DisposeAsync()
    {
        await dbContext.DisposeAsync();
        await connection.DisposeAsync();
    }
}
