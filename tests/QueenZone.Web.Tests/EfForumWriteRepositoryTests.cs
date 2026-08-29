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
    public async Task EnsureCategoryAsync_CreatesNewsBoardOnce_AndNeverReturnsTheMusic()
    {
        await SeedCategoryAsync();

        var first = await repository.EnsureCategoryAsync(
            NewsForumDiscussion.CategorySlug,
            NewsForumDiscussion.CategoryName);
        var second = await repository.EnsureCategoryAsync(
            NewsForumDiscussion.CategorySlug,
            NewsForumDiscussion.CategoryName);

        Assert.Equal(first, second);
        Assert.NotEqual(1, first);
        var created = await dbContext.ModernForumCategories.SingleAsync(category => category.LegacyForumId == first);
        Assert.Equal(NewsForumDiscussion.CategoryName, created.Name);
        Assert.False(NewsForumDiscussion.IsTheMusic(created.Name));
    }

    [Fact]
    public async Task EnsureCategoryAsync_PrefersSlugMatch_ThenCreatesWhenNameDoesNotMatch()
    {
        await SeedCategoryAsync();
        await SeedNamedCategoryAsync(3, "NEWS!");

        var first = await repository.EnsureCategoryAsync(
            NewsForumDiscussion.CategorySlug,
            NewsForumDiscussion.CategoryName);
        var second = await repository.EnsureCategoryAsync(
            NewsForumDiscussion.CategorySlug,
            NewsForumDiscussion.CategoryName);

        Assert.Equal(3, first);
        Assert.Equal(first, second);
        var matched = await dbContext.ModernForumCategories.SingleAsync(category => category.LegacyForumId == first);
        Assert.Equal("NEWS!", matched.Name);
    }

    [Fact]
    public async Task CreateThreadAsync_WritesModernForumThreadAndFirstPostAtomically()
    {
        var member = await SeedMemberAsync();
        await SeedCategoryAsync();

        var created = await repository.CreateThreadAsync(new NewForumThread(
            CategoryId: 1,
            AuthorMemberId: member.Id,
            AuthorDisplayName: member.DisplayName,
            Subject: "  A brand new topic  ",
            Body: "<p>Hello forum</p>",
            CreatedAt: DateTimeOffset.Parse("2026-07-10T04:00:00Z")));

        var thread = await dbContext.ModernForumThreads.SingleAsync(thread => thread.LegacyTopicId == created.TopicId);
        var post = await dbContext.ModernForumPosts.SingleAsync(post => post.ThreadId == thread.Id);
        var category = await dbContext.ModernForumCategories.SingleAsync(category => category.LegacyForumId == 1);
        Assert.Equal("A brand new topic", thread.Title);
        Assert.Equal(0, thread.ReplyCount);
        Assert.True(thread.IsLegacyTopicStarter);
        Assert.Equal("<p>Hello forum</p>", post.BodyHtml);
        Assert.Equal(created.TopicId, post.LegacyThreadTopicId);
        Assert.Equal(created.StarterPostId, post.LegacyPostId);
        Assert.Equal(1, category.LegacyPostCount);
        Assert.Equal(new DateTime(2026, 7, 10, 4, 0, 0, DateTimeKind.Utc), category.LastActivityAt);
    }

    [Fact]
    public async Task CreateThreadAsync_AllocatesMonotonicLegacyIdsAcrossCreates()
    {
        var member = await SeedMemberAsync();
        await SeedCategoryAsync();

        var first = await repository.CreateThreadAsync(new NewForumThread(
            1, member.Id, member.DisplayName, "First", "<p>One</p>", DateTimeOffset.UtcNow));
        var second = await repository.CreateThreadAsync(new NewForumThread(
            1, member.Id, member.DisplayName, "Second", "<p>Two</p>", DateTimeOffset.UtcNow));

        Assert.True(second.TopicId > first.TopicId);
        Assert.True(second.StarterPostId > first.StarterPostId);

        var replyId = await repository.CreatePostAsync(new NewForumPost(
            first.TopicId, member.Id, member.DisplayName, "<p>Reply</p>", DateTimeOffset.UtcNow));
        Assert.True(replyId > second.StarterPostId);
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
        var created = await repository.CreateThreadAsync(new NewForumThread(
            1,
            member.Id,
            member.DisplayName,
            "Started topic",
            "<p>First</p>",
            DateTimeOffset.Parse("2026-07-10T04:00:00Z")));

        var postId = await repository.CreatePostAsync(new NewForumPost(
            created.TopicId,
            member.Id,
            member.DisplayName,
            "<p>Reply</p>",
            DateTimeOffset.Parse("2026-07-10T04:05:00Z")));

        var thread = await dbContext.ModernForumThreads.SingleAsync(thread => thread.LegacyTopicId == created.TopicId);
        var category = await dbContext.ModernForumCategories.SingleAsync(category => category.LegacyForumId == 1);
        Assert.Equal(1, thread.ReplyCount);
        Assert.Equal(new DateTime(2026, 7, 10, 4, 5, 0, DateTimeKind.Utc), thread.LastActivityAt);
        Assert.Equal(2, category.LegacyPostCount);
        Assert.Equal(new DateTime(2026, 7, 10, 4, 5, 0, DateTimeKind.Utc), category.LastActivityAt);
        Assert.Equal("<p>Reply</p>", (await dbContext.ModernForumPosts.SingleAsync(post => post.LegacyPostId == postId)).BodyHtml);
    }

    [Fact]
    public async Task CreateThreadAsync_ThrowsWhenCategoryMissing()
    {
        var member = await SeedMemberAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.CreateThreadAsync(new NewForumThread(
            999,
            member.Id,
            member.DisplayName,
            "Missing category",
            "<p>Body</p>",
            DateTimeOffset.UtcNow)));
    }

    [Fact]
    public async Task CreatePostAsync_ThrowsWhenThreadMissing()
    {
        var member = await SeedMemberAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.CreatePostAsync(new NewForumPost(
            999,
            member.Id,
            member.DisplayName,
            "<p>Reply</p>",
            DateTimeOffset.UtcNow)));
    }

    [Fact]
    public async Task CountMethods_ReturnZeroWhenMemberMissing()
    {
        var memberId = Guid.NewGuid();

        Assert.Equal(0, await repository.CountPostsByMemberSinceAsync(memberId, DateTimeOffset.UtcNow.AddMinutes(-1)));
        Assert.Equal(0, await repository.CountApprovedPostsByMemberAsync(memberId));
    }

    [Fact]
    public async Task HideAuthorForumContentAsync_HidesAllPostsAndExcludesThemFromCounts_ThenUnhideRestoresThem()
    {
        var spammer = await SeedMemberAsync("spammer@example.com", "Spammer");
        var innocent = await SeedMemberAsync("innocent@example.com", "Innocent");
        await SeedCategoryAsync();

        var thread = await repository.CreateThreadAsync(new NewForumThread(
            1, spammer.Id, spammer.DisplayName, "Spam thread", "<p>Spam 1</p>", DateTimeOffset.UtcNow));
        await repository.CreatePostAsync(new NewForumPost(
            thread.TopicId, spammer.Id, spammer.DisplayName, "<p>Spam 2</p>", DateTimeOffset.UtcNow));
        await repository.CreatePostAsync(new NewForumPost(
            thread.TopicId, innocent.Id, innocent.DisplayName, "<p>Not spam</p>", DateTimeOffset.UtcNow));
        var innocentThread = await repository.CreateThreadAsync(new NewForumThread(
            1, innocent.Id, innocent.DisplayName, "Legitimate thread", "<p>Hello</p>", DateTimeOffset.UtcNow));
        await repository.CreatePostAsync(new NewForumPost(
            innocentThread.TopicId, spammer.Id, spammer.DisplayName, "<p>Spam reply</p>", DateTimeOffset.UtcNow));

        Assert.Equal(3, await repository.CountApprovedPostsByMemberAsync(spammer.Id));

        await repository.HideAuthorForumContentAsync(spammer.Id, spammer.DisplayName);

        // ExecuteUpdateAsync bypasses the change tracker, so re-query untracked to see the write.
        Assert.All(
            await dbContext.ModernForumPosts.AsNoTracking().Where(p => p.AuthorMemberId == spammer.Id).ToListAsync(),
            post => Assert.True(post.IsHidden));
        Assert.All(
            await dbContext.ModernForumPosts.AsNoTracking().Where(p => p.AuthorMemberId == innocent.Id).ToListAsync(),
            post => Assert.False(post.IsHidden));
        Assert.True((await dbContext.ModernForumThreads.AsNoTracking().SingleAsync(t => t.LegacyTopicId == thread.TopicId)).IsHidden);
        Assert.False((await dbContext.ModernForumThreads.AsNoTracking().SingleAsync(t => t.LegacyTopicId == innocentThread.TopicId)).IsHidden);
        Assert.Equal(0, await repository.CountApprovedPostsByMemberAsync(spammer.Id));
        Assert.Equal(2, await repository.CountApprovedPostsByMemberAsync(innocent.Id));

        await repository.UnhideAuthorForumContentAsync(spammer.Id, spammer.DisplayName);

        Assert.All(
            await dbContext.ModernForumPosts.AsNoTracking().Where(p => p.AuthorMemberId == spammer.Id).ToListAsync(),
            post => Assert.False(post.IsHidden));
        Assert.False((await dbContext.ModernForumThreads.AsNoTracking().SingleAsync(t => t.LegacyTopicId == thread.TopicId)).IsHidden);
        Assert.Equal(3, await repository.CountApprovedPostsByMemberAsync(spammer.Id));
    }

    [Fact]
    public async Task CreateThreadAsync_TruncatesBodyToLegacyColumnLimit()
    {
        var member = await SeedMemberAsync();
        await SeedCategoryAsync();
        var longBody = new string('x', 8_100);

        var created = await repository.CreateThreadAsync(new NewForumThread(
            1,
            member.Id,
            member.DisplayName,
            "Long body",
            longBody,
            DateTimeOffset.UtcNow));

        var thread = await dbContext.ModernForumThreads.SingleAsync(thread => thread.LegacyTopicId == created.TopicId);
        var post = await dbContext.ModernForumPosts.SingleAsync(post => post.ThreadId == thread.Id);
        Assert.Equal(8_000, post.BodyHtml.Length);
    }

    [Fact]
    public async Task GetThreadAsync_MapsMissingDatesToMinimum()
    {
        await SeedCategoryAsync();
        dbContext.ModernForumThreads.Add(new ModernForumThreadEntity
        {
            LegacyTopicId = 12345,
            LegacyForumId = 1,
            CategoryId = 1,
            Title = "No dates",
            StartedByDisplayName = "Forum Fan",
            ReplyCount = 0,
            IsSticky = false,
            IsLegacyTopicStarter = true,
            LegacyDiscography = 0,
            StarterAttachCount = 0,
            ImportedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        await dbContext.SaveChangesAsync();

        var thread = await repository.GetThreadAsync(12345);

        Assert.NotNull(thread);
        Assert.Equal(DateTimeOffset.MinValue, thread.CreatedAt);
        Assert.Equal(DateTimeOffset.MinValue, thread.LastPostAt);
    }

    [Fact]
    public async Task UpdatePostAsync_RejectsEditsAfterWindowExpires()
    {
        var member = await SeedMemberAsync();
        await SeedCategoryAsync();
        var created = await repository.CreateThreadAsync(new NewForumThread(
            1,
            member.Id,
            member.DisplayName,
            "Old post",
            "<p>Original</p>",
            DateTimeOffset.UtcNow.AddHours(-3)));

        var result = await repository.UpdatePostAsync(
            created.StarterPostId,
            member.Id,
            "<p>Too late</p>",
            isAdmin: false,
            editWindowMinutes: 60);

        Assert.Equal(ForumPostUpdateStatus.EditWindowExpired, result.Status);
        var post = await dbContext.ModernForumPosts.SingleAsync(item => item.LegacyPostId == created.StarterPostId);
        Assert.Equal("<p>Original</p>", post.BodyHtml);
        Assert.Equal(0, post.EditCount);
        Assert.Null(post.EditedAt);
    }

    [Fact]
    public async Task UpdatePostAsync_RejectsNonOwnerWhoIsNotAdmin()
    {
        var member = await SeedMemberAsync();
        await SeedCategoryAsync();
        var created = await repository.CreateThreadAsync(new NewForumThread(
            1,
            member.Id,
            member.DisplayName,
            "Owned",
            "<p>Original</p>",
            DateTimeOffset.UtcNow));

        var result = await repository.UpdatePostAsync(
            created.StarterPostId,
            Guid.NewGuid(),
            "<p>Nope</p>",
            isAdmin: false,
            editWindowMinutes: 60);

        Assert.Equal(ForumPostUpdateStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task UpdatePostAsync_AllowsAdminAndPersistsAuthorMemberId()
    {
        var member = await SeedMemberAsync();
        await SeedCategoryAsync();
        var created = await repository.CreateThreadAsync(new NewForumThread(
            1,
            member.Id,
            member.DisplayName,
            "Admin target",
            "<p>Original</p>",
            DateTimeOffset.UtcNow.AddDays(-10)));

        var stored = await dbContext.ModernForumPosts.SingleAsync(item => item.LegacyPostId == created.StarterPostId);
        Assert.Equal(member.Id, stored.AuthorMemberId);

        var result = await repository.UpdatePostAsync(
            created.StarterPostId,
            Guid.NewGuid(),
            "<p>Admin rewrite</p>",
            isAdmin: true,
            editWindowMinutes: 60);

        Assert.Equal(ForumPostUpdateStatus.Success, result.Status);
        await dbContext.Entry(stored).ReloadAsync();
        Assert.Equal("<p>Admin rewrite</p>", stored.BodyHtml);
        Assert.Equal(1, stored.EditCount);
        Assert.NotNull(stored.EditedAt);
    }

    [Fact]
    public async Task HideAuthorForumContentAsync_HidesUnlinkedDisplayName_AndLeavesOtherMemberIds()
    {
        var innocent = await SeedMemberAsync("innocent@example.com", "PatriciaCMardis");
        await SeedCategoryAsync();
        var createdAt = DateTimeOffset.UtcNow;

        var unlinked = await repository.CreateThreadAsync(new NewForumThread(
            1, innocent.Id, "PatriciaCMardis", "Unlinked spam", "<p>Spam</p>", createdAt));
        await UnlinkAuthorAsync(unlinked.StarterPostId);
        var mixed = await repository.CreateThreadAsync(new NewForumThread(
            1, innocent.Id, innocent.DisplayName, "Mixed thread", "<p>Real opener</p>", createdAt));
        var spamReply = await repository.CreatePostAsync(new NewForumPost(
            mixed.TopicId, innocent.Id, "PatriciaCMardis", "<p>Spam reply</p>", createdAt));
        await UnlinkAuthorAsync(spamReply);
        var sameNameMemberThread = await repository.CreateThreadAsync(new NewForumThread(
            1, innocent.Id, innocent.DisplayName, "Member-owned same name", "<p>Keep</p>", createdAt));

        var found = await repository.FindForumAuthorByDisplayNameAsync("patriciacmardis");
        Assert.NotNull(found);
        Assert.Equal(2, found.PostCount);
        Assert.Equal(1, found.ThreadCount);

        await repository.HideAuthorForumContentAsync(null, "PatriciaCMardis");

        Assert.True((await dbContext.ModernForumPosts.AsNoTracking().SingleAsync(p => p.LegacyPostId == unlinked.StarterPostId)).IsHidden);
        Assert.True((await dbContext.ModernForumPosts.AsNoTracking().SingleAsync(p => p.LegacyPostId == spamReply)).IsHidden);
        Assert.False((await dbContext.ModernForumPosts.AsNoTracking().SingleAsync(p => p.LegacyPostId == mixed.StarterPostId)).IsHidden);
        Assert.False((await dbContext.ModernForumPosts.AsNoTracking().SingleAsync(p => p.LegacyPostId == sameNameMemberThread.StarterPostId)).IsHidden);
        Assert.True((await dbContext.ModernForumThreads.AsNoTracking().SingleAsync(t => t.LegacyTopicId == unlinked.TopicId)).IsHidden);
        Assert.False((await dbContext.ModernForumThreads.AsNoTracking().SingleAsync(t => t.LegacyTopicId == mixed.TopicId)).IsHidden);
        Assert.False((await dbContext.ModernForumThreads.AsNoTracking().SingleAsync(t => t.LegacyTopicId == sameNameMemberThread.TopicId)).IsHidden);

        await repository.UnhideAuthorForumContentAsync(null, "PatriciaCMardis");

        Assert.False((await dbContext.ModernForumPosts.AsNoTracking().SingleAsync(p => p.LegacyPostId == unlinked.StarterPostId)).IsHidden);
        Assert.False((await dbContext.ModernForumThreads.AsNoTracking().SingleAsync(t => t.LegacyTopicId == unlinked.TopicId)).IsHidden);
    }

    [Fact]
    public async Task HideAuthorForumContentAsync_WithMemberId_AlsoHidesUnlinkedSameDisplayName()
    {
        var spammer = await SeedMemberAsync("spammer@example.com", "Spammer");
        var innocent = await SeedMemberAsync("innocent@example.com", "Innocent");
        await SeedCategoryAsync();
        var createdAt = DateTimeOffset.UtcNow;

        var linked = await repository.CreateThreadAsync(new NewForumThread(
            1, spammer.Id, spammer.DisplayName, "Linked spam", "<p>Spam</p>", createdAt));
        var unlinked = await repository.CreateThreadAsync(new NewForumThread(
            1, innocent.Id, "Spammer", "Unlinked same name", "<p>Old spam</p>", createdAt));
        await UnlinkAuthorAsync(unlinked.StarterPostId);
        var innocentThread = await repository.CreateThreadAsync(new NewForumThread(
            1, innocent.Id, innocent.DisplayName, "Innocent topic", "<p>Hello</p>", createdAt));

        await repository.HideAuthorForumContentAsync(spammer.Id, "spammer");

        Assert.True((await dbContext.ModernForumPosts.AsNoTracking().SingleAsync(p => p.LegacyPostId == linked.StarterPostId)).IsHidden);
        Assert.True((await dbContext.ModernForumPosts.AsNoTracking().SingleAsync(p => p.LegacyPostId == unlinked.StarterPostId)).IsHidden);
        Assert.False((await dbContext.ModernForumPosts.AsNoTracking().SingleAsync(p => p.LegacyPostId == innocentThread.StarterPostId)).IsHidden);
        Assert.True((await dbContext.ModernForumThreads.AsNoTracking().SingleAsync(t => t.LegacyTopicId == linked.TopicId)).IsHidden);
        Assert.True((await dbContext.ModernForumThreads.AsNoTracking().SingleAsync(t => t.LegacyTopicId == unlinked.TopicId)).IsHidden);
        Assert.False((await dbContext.ModernForumThreads.AsNoTracking().SingleAsync(t => t.LegacyTopicId == innocentThread.TopicId)).IsHidden);
    }

    public async ValueTask DisposeAsync()
    {
        await dbContext.DisposeAsync();
        await connection.DisposeAsync();
    }

    private async Task<MemberAccount> SeedMemberAsync(string email = "fan@example.com", string displayName = "Forum Fan")
    {
        var member = new MemberAccount
        {
            Id = Guid.NewGuid(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            DisplayName = displayName,
            CreatedAt = DateTime.UtcNow,
        };
        dbContext.MemberAccounts.Add(member);
        await dbContext.SaveChangesAsync();
        return member;
    }

    private async Task UnlinkAuthorAsync(int postId)
    {
        var post = await dbContext.ModernForumPosts.SingleAsync(item => item.LegacyPostId == postId);
        post.AuthorMemberId = null;
        await dbContext.SaveChangesAsync();
    }

    private async Task SeedCategoryAsync()
    {
        await SeedNamedCategoryAsync(1, "The Music");
    }

    private async Task SeedNamedCategoryAsync(int legacyForumId, string name)
    {
        dbContext.ModernForumCategories.Add(new ModernForumCategoryEntity
        {
            LegacyForumId = legacyForumId,
            Name = name,
            SortOrder = legacyForumId,
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
                IsHidden INTEGER NOT NULL DEFAULT 0,
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
                AuthorMemberId TEXT NULL,
                EditedAt TEXT NULL,
                EditCount INTEGER NOT NULL DEFAULT 0,
                IsHidden INTEGER NOT NULL DEFAULT 0,
                ImportedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                FOREIGN KEY (ThreadId) REFERENCES ModernForumThread (Id)
            );
            """);
    }
}
