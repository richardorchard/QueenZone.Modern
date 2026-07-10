using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QueenZone.Data;
using QueenZone.Data.Entities;

namespace QueenZone.Web.Tests;

public sealed class EfForumWriteRepositoryTests : IAsyncDisposable
{
    private readonly SqliteConnection connection = new("DataSource=:memory:");
    private readonly QueenZoneDbContext dbContext;
    private readonly EfForumWriteRepository repository;

    public EfForumWriteRepositoryTests()
    {
        connection.Open();
        var options = new DbContextOptionsBuilder<QueenZoneDbContext>()
            .UseSqlite(connection)
            .Options;
        dbContext = new QueenZoneDbContext(options);
        dbContext.Database.EnsureCreated();
        CreateModernForumTables();
        repository = new EfForumWriteRepository(dbContext);
    }

    [Fact]
    public async Task CreateThreadAsync_WritesModernForumThreadAndFirstPostAtomically()
    {
        var member = await SeedMemberAsync();
        await SeedCategoryAsync();

        var topicId = await repository.CreateThreadAsync(new NewForumThread(
            CategoryId: 1,
            AuthorMemberId: member.Id,
            AuthorDisplayName: member.DisplayName,
            Subject: "  A brand new topic  ",
            Body: "<p>Hello forum</p>",
            CreatedAt: DateTimeOffset.Parse("2026-07-10T04:00:00Z")));

        var thread = await dbContext.ModernForumThreads.SingleAsync(thread => thread.LegacyTopicId == topicId);
        var post = await dbContext.ModernForumPosts.SingleAsync(post => post.ThreadId == thread.Id);
        Assert.Equal("A brand new topic", thread.Title);
        Assert.Equal(0, thread.ReplyCount);
        Assert.True(thread.IsLegacyTopicStarter);
        Assert.Equal("<p>Hello forum</p>", post.BodyHtml);
        Assert.Equal(topicId, post.LegacyThreadTopicId);
    }

    [Fact]
    public async Task CreateThreadAsync_RollsBack_WhenFirstPostCannotBeInserted()
    {
        var member = await SeedMemberAsync();
        await SeedCategoryAsync();

        await Assert.ThrowsAnyAsync<Exception>(() => repository.CreateThreadAsync(new NewForumThread(
            CategoryId: 1,
            AuthorMemberId: member.Id,
            AuthorDisplayName: member.DisplayName,
            Subject: "Broken topic",
            Body: null!,
            CreatedAt: DateTimeOffset.UtcNow)));

        Assert.Empty(dbContext.ModernForumThreads);
        Assert.Empty(dbContext.ModernForumPosts);
    }

    [Fact]
    public async Task CreatePostAsync_AppendsModernForumPostAndUpdatesThread()
    {
        var member = await SeedMemberAsync();
        await SeedCategoryAsync();
        var topicId = await repository.CreateThreadAsync(new NewForumThread(
            1,
            member.Id,
            member.DisplayName,
            "Started topic",
            "<p>First</p>",
            DateTimeOffset.Parse("2026-07-10T04:00:00Z")));

        var postId = await repository.CreatePostAsync(new NewForumPost(
            topicId,
            member.Id,
            member.DisplayName,
            "<p>Reply</p>",
            DateTimeOffset.Parse("2026-07-10T04:05:00Z")));

        var thread = await dbContext.ModernForumThreads.SingleAsync(thread => thread.LegacyTopicId == topicId);
        Assert.Equal(1, thread.ReplyCount);
        Assert.Equal("<p>Reply</p>", (await dbContext.ModernForumPosts.SingleAsync(post => post.LegacyPostId == postId)).BodyHtml);
    }

    public async ValueTask DisposeAsync()
    {
        await dbContext.DisposeAsync();
        await connection.DisposeAsync();
    }

    private async Task<MemberAccount> SeedMemberAsync()
    {
        var member = new MemberAccount
        {
            Id = Guid.NewGuid(),
            Email = "fan@example.com",
            NormalizedEmail = "FAN@EXAMPLE.COM",
            DisplayName = "Forum Fan",
            CreatedAt = DateTime.UtcNow,
        };
        dbContext.MemberAccounts.Add(member);
        await dbContext.SaveChangesAsync();
        return member;
    }

    private async Task SeedCategoryAsync()
    {
        dbContext.ModernForumCategories.Add(new ModernForumCategoryEntity
        {
            LegacyForumId = 1,
            Name = "The Music",
            SortOrder = 1,
            LegacyPostCount = 0,
            ImportedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        await dbContext.SaveChangesAsync();
    }

    private void CreateModernForumTables()
    {
        dbContext.Database.ExecuteSqlRaw("""
            CREATE TABLE ModernForumCategory
            (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                LegacyForumId INTEGER NOT NULL UNIQUE,
                Name TEXT NOT NULL,
                Description TEXT NULL,
                SortOrder INTEGER NOT NULL,
                LegacyPostCount INTEGER NOT NULL,
                LastActivityAt TEXT NULL,
                IsSynthetic INTEGER NOT NULL DEFAULT 0,
                ImportedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );

            CREATE TABLE ModernForumThread
            (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                LegacyTopicId INTEGER NOT NULL UNIQUE,
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
                UpdatedAt TEXT NOT NULL,
                FOREIGN KEY (CategoryId) REFERENCES ModernForumCategory (Id)
            );

            CREATE TABLE ModernForumPost
            (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                LegacyPostId INTEGER NOT NULL UNIQUE,
                LegacyThreadTopicId INTEGER NOT NULL,
                ThreadId INTEGER NOT NULL,
                LegacyForumId INTEGER NOT NULL,
                AuthorLegacyUserId INTEGER NULL,
                AuthorDisplayName TEXT NOT NULL,
                AuthorPostCount INTEGER NULL,
                AuthorJoinedAt TEXT NULL,
                BodyHtml TEXT NOT NULL,
                SignatureHtml TEXT NULL,
                PostedAt TEXT NULL,
                LegacyDiscography INTEGER NOT NULL,
                AuthorUserValidated INTEGER NULL,
                Attachment TEXT NULL,
                FileSize TEXT NULL,
                AttachCount INTEGER NOT NULL,
                ImportedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                FOREIGN KEY (ThreadId) REFERENCES ModernForumThread (Id)
            );
            """);
    }
}
