using QueenZone.Data;
using QueenZone.Data.Entities;

namespace QueenZone.Web.Tests;

public sealed class NotificationPreferenceRepositoryTests
{
    [Fact]
    public async Task GetAsync_UnknownMember_ReturnsDefaults()
    {
        var repository = CreateRepository();

        var snapshot = await repository.GetAsync(Guid.NewGuid());

        Assert.Equal(NotificationPreferences.Defaults, snapshot);
    }

    [Fact]
    public async Task FilterEnabledAsync_News_DefaultOn_IncludesMembersWithoutARow()
    {
        var repository = CreateRepository();
        var absent = Guid.NewGuid();
        var muted = Guid.NewGuid();
        var confirmed = Guid.NewGuid();

        await repository.ApplyAsync(muted, new NotificationPreferencePatch(null, null, false));
        await repository.ApplyAsync(confirmed, new NotificationPreferencePatch(null, null, true));

        var filtered = await repository.FilterEnabledAsync(
            [absent, muted, confirmed],
            NotificationCategory.News);

        Assert.Equal([absent, confirmed], filtered);
    }

    [Fact]
    public async Task FilterEnabledAsync_ForumReply_DefaultOn_IncludesMembersWithoutARow()
    {
        var repository = CreateRepository();
        var absent = Guid.NewGuid();
        var muted = Guid.NewGuid();
        var confirmed = Guid.NewGuid();

        await repository.ApplyAsync(muted, new NotificationPreferencePatch(false, null, null));
        await repository.ApplyAsync(confirmed, new NotificationPreferencePatch(true, null, null));

        var filtered = await repository.FilterEnabledAsync(
            [absent, muted, confirmed],
            NotificationCategory.ForumReply);

        Assert.Equal([absent, confirmed], filtered);
    }

    [Fact]
    public async Task FilterEnabledAsync_EmptySet_ReturnsEmpty()
    {
        var repository = CreateRepository();

        var filtered = await repository.FilterEnabledAsync([], NotificationCategory.PrivateMessage);

        Assert.Empty(filtered);
    }

    [Fact]
    public async Task ListEnabledAsync_News_ReturnsOnlyExplicitEnabledRows()
    {
        var repository = CreateRepository();
        var enabled = Guid.NewGuid();
        var muted = Guid.NewGuid();
        var forumOnly = Guid.NewGuid();

        await repository.ApplyAsync(enabled, new NotificationPreferencePatch(null, null, true));
        await repository.ApplyAsync(muted, new NotificationPreferencePatch(null, null, false));
        await repository.ApplyAsync(forumOnly, new NotificationPreferencePatch(true, null, null));

        var listed = await repository.ListEnabledAsync(NotificationCategory.News);

        Assert.Equal([enabled], listed);
    }

    [Fact]
    public async Task ListEnabledAsync_EmptyStore_ReturnsEmpty()
    {
        var repository = CreateRepository();

        var listed = await repository.ListEnabledAsync(NotificationCategory.News);

        Assert.Empty(listed);
    }

    [Fact]
    public async Task ApplyAsync_KeepsRow_WhenValueMatchesDefault()
    {
        var store = new SharedNotificationPreferenceStore();
        var repository = new InMemoryNotificationPreferenceRepository(store);
        var memberId = Guid.NewGuid();

        await repository.ApplyAsync(memberId, new NotificationPreferencePatch(true, null, null));

        lock (store.Gate)
        {
            var row = Assert.Single(store.Rows);
            Assert.Equal(memberId, row.MemberAccountId);
            Assert.Equal(NotificationCategory.ForumReply, row.Category);
            Assert.True(row.IsEnabled);
        }

        var snapshot = await repository.GetAsync(memberId);
        Assert.True(snapshot.ForumReply);
    }

    private static InMemoryNotificationPreferenceRepository CreateRepository() =>
        new(new SharedNotificationPreferenceStore());
}
