using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class InMemoryForumWriteRepositoryTests
{
    [Fact]
    public async Task CreatePostAsync_AttachesReplyToSeedTopic()
    {
        var repository = new InMemoryForumWriteRepository();
        var memberId = Guid.NewGuid();

        var postId = await repository.CreatePostAsync(new NewForumPost(
            1002,
            memberId,
            "Forum Fan",
            "<p>Reply</p>",
            DateTimeOffset.UtcNow));

        var thread = await repository.GetThreadAsync(1002);
        Assert.True(postId >= 2_000_000);
        Assert.NotNull(thread);
        Assert.Equal(27, thread.PostCount);
    }

    [Fact]
    public async Task CreatePostAsync_ThrowsForUnknownTopic()
    {
        var repository = new InMemoryForumWriteRepository();

        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.CreatePostAsync(new NewForumPost(
            9999,
            Guid.NewGuid(),
            "Forum Fan",
            "<p>Reply</p>",
            DateTimeOffset.UtcNow)));
    }

    [Fact]
    public async Task CountMethods_ReturnStoredPostCounts()
    {
        var repository = new InMemoryForumWriteRepository();
        var memberId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;

        await repository.CreateThreadAsync(new NewForumThread(
            1,
            memberId,
            "Forum Fan",
            "New topic",
            "<p>Body</p>",
            createdAt));

        Assert.Equal(1, await repository.CountPostsByMemberSinceAsync(memberId, createdAt.AddSeconds(-1)));
        Assert.Equal(0, await repository.CountPostsByMemberSinceAsync(memberId, createdAt.AddSeconds(1)));
        Assert.Equal(1, await repository.CountApprovedPostsByMemberAsync(memberId));
    }

    [Fact]
    public async Task HidePostsByMemberAsync_ExcludesPostsFromTopicView_UnhideRestoresThem()
    {
        var repository = new InMemoryForumWriteRepository();
        var spammerId = Guid.NewGuid();
        var innocentId = Guid.NewGuid();

        var thread = await repository.CreateThreadAsync(new NewForumThread(
            1, spammerId, "Spammer", "Spam thread", "<p>Spam 1</p>", DateTimeOffset.UtcNow));
        await repository.CreatePostAsync(new NewForumPost(
            thread.TopicId, innocentId, "Innocent", "<p>Not spam</p>", DateTimeOffset.UtcNow));

        Assert.Equal(2, repository.GetPostsForTopic(thread.TopicId).Count);
        Assert.Equal(1, await repository.CountApprovedPostsByMemberAsync(spammerId));

        await repository.HideAuthorForumContentAsync(spammerId, "Spammer");

        var visiblePosts = repository.GetPostsForTopic(thread.TopicId);
        Assert.Single(visiblePosts);
        Assert.DoesNotContain(visiblePosts, post => post.MemberId == spammerId);
        Assert.DoesNotContain(repository.GetCreatedThreads(), item => item.TopicId == thread.TopicId);
        Assert.Equal(0, await repository.CountApprovedPostsByMemberAsync(spammerId));

        await repository.UnhideAuthorForumContentAsync(spammerId, "Spammer");

        Assert.Equal(2, repository.GetPostsForTopic(thread.TopicId).Count);
        Assert.Contains(repository.GetCreatedThreads(), item => item.TopicId == thread.TopicId);
        Assert.Equal(1, await repository.CountApprovedPostsByMemberAsync(spammerId));
    }

    [Fact]
    public async Task InMemoryForumRepository_ReflectsCreatedThreadsInCategoryStats()
    {
        var writeRepository = new InMemoryForumWriteRepository();
        var repository = new InMemoryForumRepository(
            [
                new ForumCategoryItem(
                    1,
                    "The Music",
                    "Albums and songs.",
                    10,
                    new DateTime(2020, 10, 6, 0, 0, 0, DateTimeKind.Utc),
                    "Older topic",
                    1),
            ],
            new ForumArchiveStats(1, 3, 10),
            writeRepository);
        var createdAt = DateTimeOffset.Parse("2026-07-10T04:00:00Z");

        await writeRepository.CreateThreadAsync(new NewForumThread(
            1,
            Guid.NewGuid(),
            "Forum Fan",
            "Fresh activity",
            "<p>Body</p>",
            createdAt));

        var categories = await repository.GetCategoriesAsync();
        var threadCount = await repository.GetTotalThreadCountAsync();
        var stats = await repository.GetArchiveStatsAsync();

        var category = Assert.Single(categories);
        Assert.Equal(11, category.PostCount);
        Assert.Equal(createdAt.UtcDateTime, category.LastActivityAt);
        Assert.Equal("Fresh activity", category.LatestThreadTitle);
        Assert.Equal(4, threadCount);
        Assert.Equal(11, stats.PostCount);
        Assert.Equal(4, stats.ThreadCount);
    }

    [Fact]
    public async Task UpdatePostAsync_RejectsEditsAfterWindowExpires()
    {
        var repository = new InMemoryForumWriteRepository();
        var memberId = Guid.NewGuid();
        var created = await repository.CreateThreadAsync(new NewForumThread(
            1,
            memberId,
            "Forum Fan",
            "Editable topic",
            "<p>Original</p>",
            DateTimeOffset.UtcNow.AddHours(-2)));

        var result = await repository.UpdatePostAsync(
            created.StarterPostId,
            memberId,
            "<p>Too late</p>",
            isAdmin: false,
            editWindowMinutes: 60);

        Assert.Equal(ForumPostUpdateStatus.EditWindowExpired, result.Status);
        var post = await repository.GetPostAsync(created.StarterPostId);
        Assert.Equal("<p>Original</p>", post!.Body);
        Assert.Equal(0, post.EditCount);
    }

    [Fact]
    public async Task UpdatePostAsync_rejects_stale_updated_at()
    {
        var repository = new InMemoryForumWriteRepository();
        var memberId = Guid.NewGuid();
        var created = await repository.CreateThreadAsync(new NewForumThread(
            1,
            memberId,
            "Forum Fan",
            "Editable topic",
            "<p>Original</p>",
            DateTimeOffset.UtcNow));
        var original = await repository.GetPostAsync(created.StarterPostId);

        var first = await repository.UpdatePostAsync(
            created.StarterPostId,
            memberId,
            "<p>First writer</p>",
            isAdmin: false,
            editWindowMinutes: 60,
            original!.UpdatedAt);
        Assert.Equal(ForumPostUpdateStatus.Success, first.Status);

        var stale = await repository.UpdatePostAsync(
            created.StarterPostId,
            memberId,
            "<p>Stale overwrite</p>",
            isAdmin: false,
            editWindowMinutes: 60,
            original.UpdatedAt);
        Assert.Equal(ForumPostUpdateStatus.ConcurrencyConflict, stale.Status);
        var post = await repository.GetPostAsync(created.StarterPostId);
        Assert.Equal("<p>First writer</p>", post!.Body);
    }

    [Fact]
    public async Task UpdatePostAsync_RejectsNonOwnerWhoIsNotAdmin()
    {
        var repository = new InMemoryForumWriteRepository();
        var ownerId = Guid.NewGuid();
        var created = await repository.CreateThreadAsync(new NewForumThread(
            1,
            ownerId,
            "Owner",
            "Owned topic",
            "<p>Original</p>",
            DateTimeOffset.UtcNow));

        var result = await repository.UpdatePostAsync(
            created.StarterPostId,
            Guid.NewGuid(),
            "<p>Hijack</p>",
            isAdmin: false,
            editWindowMinutes: 60);

        Assert.Equal(ForumPostUpdateStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task UpdatePostAsync_AllowsAdminRegardlessOfAgeOrOwner()
    {
        var repository = new InMemoryForumWriteRepository();
        var ownerId = Guid.NewGuid();
        var created = await repository.CreateThreadAsync(new NewForumThread(
            1,
            ownerId,
            "Owner",
            "Old topic",
            "<p>Original</p>",
            DateTimeOffset.UtcNow.AddDays(-30)));

        var result = await repository.UpdatePostAsync(
            created.StarterPostId,
            Guid.NewGuid(),
            "<p>Admin edit</p>",
            isAdmin: true,
            editWindowMinutes: 60);

        Assert.Equal(ForumPostUpdateStatus.Success, result.Status);
        var post = await repository.GetPostAsync(created.StarterPostId);
        Assert.Equal("<p>Admin edit</p>", post!.Body);
        Assert.Equal(1, post.EditCount);
        Assert.NotNull(post.EditedAt);
    }

    [Fact]
    public async Task EnsureCategoryAsync_CreatesNewsBoardOnce_AndNeverReturnsTheMusic()
    {
        var repository = new InMemoryForumWriteRepository();

        var first = await repository.EnsureCategoryAsync(
            NewsForumDiscussion.CategorySlug,
            NewsForumDiscussion.CategoryName);
        var second = await repository.EnsureCategoryAsync(
            NewsForumDiscussion.CategorySlug,
            NewsForumDiscussion.CategoryName);

        Assert.Equal(first, second);
        Assert.NotEqual(1, first);
        var created = Assert.Single(repository.GetCreatedCategories());
        Assert.Equal(first, created.Id);
        Assert.Equal(NewsForumDiscussion.CategoryName, created.Name);
        Assert.False(NewsForumDiscussion.IsTheMusic(created.Name));
    }

    [Fact]
    public async Task EnsureCategoryAsync_PrefersSlugMatch_ThenName_AndCreatesWhenMissing()
    {
        var repository = new InMemoryForumWriteRepository();

        var slugId = await repository.EnsureCategoryAsync(
            NewsForumDiscussion.CategorySlug,
            "NEWS!");
        var sameBySlug = await repository.EnsureCategoryAsync(
            NewsForumDiscussion.CategorySlug,
            NewsForumDiscussion.CategoryName);
        Assert.Equal(slugId, sameBySlug);
        Assert.Equal("NEWS!", Assert.Single(repository.GetCreatedCategories()).Name);

        var other = new InMemoryForumWriteRepository();
        var deskId = await other.EnsureCategoryAsync("news-desk", "News Desk");
        var newsId = await other.EnsureCategoryAsync(
            NewsForumDiscussion.CategorySlug,
            NewsForumDiscussion.CategoryName);
        Assert.NotEqual(deskId, newsId);
        Assert.Equal(
            NewsForumDiscussion.CategoryName,
            other.GetCreatedCategories().Single(category => category.Id == newsId).Name);
    }
}
