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
}
