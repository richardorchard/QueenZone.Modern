using QueenZone.Data;
using QueenZone.Data.Entities;

namespace QueenZone.Web.Tests;

public sealed class InMemoryPrivateMessageRepositoryTests
{
    [Fact]
    public async Task Inbox_IsOrderedByMostRecentSortKey_AndIsolatedPerParticipant()
    {
        var members = new InMemoryMemberAccountRepository();
        var alice = await members.CreateAsync(NewMember("a@example.com", "Alice"));
        var bob = await members.CreateAsync(NewMember("b@example.com", "Bob"));
        var carol = await members.CreateAsync(NewMember("c@example.com", "Carol"));
        var repo = new InMemoryPrivateMessageRepository(id =>
            members.FindByIdAsync(id).GetAwaiter().GetResult());

        var older = DateTimeOffset.Parse("2026-08-01T10:00:00Z");
        var newer = DateTimeOffset.Parse("2026-08-01T12:00:00Z");

        await repo.SendNewOrExistingAsync(alice.Id, bob.Id, "To Bob", older);
        await repo.SendNewOrExistingAsync(alice.Id, carol.Id, "To Carol", newer);

        var aliceInbox = await repo.GetInboxAsync(alice.Id);
        Assert.Equal(["Carol", "Bob"], aliceInbox.Items.Select(i => i.OtherParticipantDisplayName).ToArray());

        var bobInbox = await repo.GetInboxAsync(bob.Id);
        Assert.Equal(["Alice"], bobInbox.Items.Select(i => i.OtherParticipantDisplayName).ToArray());
        Assert.DoesNotContain(bobInbox.Items, i => i.OtherParticipantId == carol.Id);
    }

    [Fact]
    public async Task Inbox_OrdersByLastMessageSortKey_EvenWhenTimestampsSkew()
    {
        var members = new InMemoryMemberAccountRepository();
        var alice = await members.CreateAsync(NewMember("a-skew@example.com", "Alice"));
        var bob = await members.CreateAsync(NewMember("b-skew@example.com", "Bob"));
        var carol = await members.CreateAsync(NewMember("c-skew@example.com", "Carol"));
        var repo = new InMemoryPrivateMessageRepository(id =>
            members.FindByIdAsync(id).GetAwaiter().GetResult());

        await repo.SendNewOrExistingAsync(
            alice.Id,
            carol.Id,
            "Carol first insert",
            DateTimeOffset.Parse("2026-08-01T20:00:00Z"));
        await repo.SendNewOrExistingAsync(
            alice.Id,
            bob.Id,
            "Bob later insert, earlier clock",
            DateTimeOffset.Parse("2026-08-01T08:00:00Z"));

        var aliceInbox = await repo.GetInboxAsync(alice.Id);
        Assert.Equal(["Bob", "Carol"], aliceInbox.Items.Select(i => i.OtherParticipantDisplayName).ToArray());
        Assert.True(aliceInbox.Items[0].LastMessageAt < aliceInbox.Items[1].LastMessageAt);
    }

    [Fact]
    public async Task Preview_IsPlainTextTruncated()
    {
        var members = new InMemoryMemberAccountRepository();
        var alice = await members.CreateAsync(NewMember("a2@example.com", "Alice"));
        var bob = await members.CreateAsync(NewMember("b2@example.com", "Bob"));
        var repo = new InMemoryPrivateMessageRepository(id =>
            members.FindByIdAsync(id).GetAwaiter().GetResult());

        var body = new string('x', PrivateMessageLimits.PreviewLength + 40);
        await repo.SendNewOrExistingAsync(alice.Id, bob.Id, body, DateTimeOffset.UtcNow);

        var item = Assert.Single((await repo.GetInboxAsync(bob.Id)).Items);
        Assert.Equal(PrivateMessageLimits.PreviewLength, item.LastMessagePreview.Length);
        Assert.DoesNotContain('<', item.LastMessagePreview);
    }

    [Fact]
    public async Task GetConversation_PagesMessages_DefaultingToLatestPage()
    {
        var members = new InMemoryMemberAccountRepository();
        var alice = await members.CreateAsync(NewMember("a-page@example.com", "Alice"));
        var bob = await members.CreateAsync(NewMember("b-page@example.com", "Bob"));
        var repo = new InMemoryPrivateMessageRepository(id =>
            members.FindByIdAsync(id).GetAwaiter().GetResult());

        var created = await repo.SendNewOrExistingAsync(
            alice.Id,
            bob.Id,
            "Msg 1",
            DateTimeOffset.Parse("2026-08-01T11:00:00Z"));
        var conversationId = created.ConversationId!.Value;
        for (var i = 2; i <= 5; i++)
        {
            await repo.ReplyAsync(
                conversationId,
                alice.Id,
                $"Msg {i}",
                DateTimeOffset.Parse("2026-08-01T11:00:00Z").AddMinutes(i));
        }

        var latest = await repo.GetConversationAsync(conversationId, bob.Id, page: null, pageSize: 2);
        Assert.Equal(3, latest!.Page);
        Assert.Equal(["Msg 4", "Msg 5"], latest.Messages.Select(m => m.Body).ToArray());

        var first = await repo.GetConversationAsync(conversationId, bob.Id, page: 1, pageSize: 2);
        Assert.Equal(["Msg 1", "Msg 2"], first!.Messages.Select(m => m.Body).ToArray());
    }

    [Fact]
    public async Task Reply_UpdatesPreviewAndSortKeyTip_KeepsMonotonicLastMessageAt()
    {
        var members = new InMemoryMemberAccountRepository();
        var alice = await members.CreateAsync(NewMember("a3@example.com", "Alice"));
        var bob = await members.CreateAsync(NewMember("b3@example.com", "Bob"));
        var repo = new InMemoryPrivateMessageRepository(id =>
            members.FindByIdAsync(id).GetAwaiter().GetResult());

        var created = await repo.SendNewOrExistingAsync(
            alice.Id,
            bob.Id,
            "Start",
            DateTimeOffset.Parse("2026-08-01T10:00:00Z"));
        var conversationId = created.ConversationId!.Value;

        await repo.ReplyAsync(
            conversationId,
            bob.Id,
            "Newer",
            DateTimeOffset.Parse("2026-08-01T10:02:00Z"));
        await repo.ReplyAsync(
            conversationId,
            alice.Id,
            "Older",
            DateTimeOffset.Parse("2026-08-01T10:01:00Z"));

        var item = Assert.Single((await repo.GetInboxAsync(alice.Id)).Items);
        // Tip-by-SortKey preview; LastMessageAt remains the max observed timestamp.
        Assert.Equal("Older", item.LastMessagePreview);
        Assert.Equal(DateTimeOffset.Parse("2026-08-01T10:02:00Z"), item.LastMessageAt);
    }

    [Fact]
    public async Task GetInbox_PagesConversations()
    {
        var members = new InMemoryMemberAccountRepository();
        var alice = await members.CreateAsync(NewMember("a-inbox@example.com", "Alice"));
        var repo = new InMemoryPrivateMessageRepository(id =>
            members.FindByIdAsync(id).GetAwaiter().GetResult());

        for (var i = 1; i <= 5; i++)
        {
            var peer = await members.CreateAsync(NewMember($"peer-inbox{i}@example.com", $"Peer {i}"));
            await repo.SendNewOrExistingAsync(
                alice.Id,
                peer.Id,
                $"Hello {i}",
                DateTimeOffset.Parse("2026-08-01T12:00:00Z").AddMinutes(i));
        }

        var page1 = await repo.GetInboxAsync(alice.Id, page: 1, pageSize: 2);
        Assert.Equal(5, page1.TotalCount);
        Assert.Equal(2, page1.Items.Count);
        Assert.Equal("Peer 5", page1.Items[0].OtherParticipantDisplayName);

        var page3 = await repo.GetInboxAsync(alice.Id, page: 3, pageSize: 2);
        Assert.Equal(3, page3.Page);
        Assert.Single(page3.Items);
    }

    [Fact]
    public async Task Archive_HidesConversationFromInbox_ButNotForOtherParticipant()
    {
        var members = new InMemoryMemberAccountRepository();
        var alice = await members.CreateAsync(NewMember("a-archive@example.com", "Alice"));
        var bob = await members.CreateAsync(NewMember("b-archive@example.com", "Bob"));
        var carol = await members.CreateAsync(NewMember("c-archive@example.com", "Carol"));
        var repo = new InMemoryPrivateMessageRepository(id =>
            members.FindByIdAsync(id).GetAwaiter().GetResult());

        var created = await repo.SendNewOrExistingAsync(
            alice.Id,
            bob.Id,
            "Archive me",
            DateTimeOffset.Parse("2026-08-01T09:00:00Z"));
        var conversationId = created.ConversationId!.Value;

        Assert.True(await repo.ArchiveConversationAsync(conversationId, alice.Id));
        Assert.False(await repo.ArchiveConversationAsync(conversationId, carol.Id));

        Assert.Empty((await repo.GetInboxAsync(alice.Id)).Items);
        Assert.Single((await repo.GetInboxAsync(bob.Id)).Items);

        var archived = Assert.Single((await repo.GetArchivedInboxAsync(alice.Id)).Items);
        Assert.Equal(bob.Id, archived.OtherParticipantId);
    }

    [Fact]
    public async Task NewMessage_UnarchivesConversation_ForBothParticipants()
    {
        var members = new InMemoryMemberAccountRepository();
        var alice = await members.CreateAsync(NewMember("a-reopen@example.com", "Alice"));
        var bob = await members.CreateAsync(NewMember("b-reopen@example.com", "Bob"));
        var repo = new InMemoryPrivateMessageRepository(id =>
            members.FindByIdAsync(id).GetAwaiter().GetResult());

        var created = await repo.SendNewOrExistingAsync(
            alice.Id,
            bob.Id,
            "Start",
            DateTimeOffset.Parse("2026-08-01T09:10:00Z"));
        var conversationId = created.ConversationId!.Value;

        await repo.ArchiveConversationAsync(conversationId, alice.Id);
        Assert.Empty((await repo.GetInboxAsync(alice.Id)).Items);

        await repo.ReplyAsync(
            conversationId,
            bob.Id,
            "Reopens it",
            DateTimeOffset.Parse("2026-08-01T09:11:00Z"));

        Assert.Single((await repo.GetInboxAsync(alice.Id)).Items);
        Assert.Empty((await repo.GetArchivedInboxAsync(alice.Id)).Items);
    }

    [Fact]
    public async Task Unarchive_MovesConversationBackToInbox()
    {
        var members = new InMemoryMemberAccountRepository();
        var alice = await members.CreateAsync(NewMember("a-unarchive@example.com", "Alice"));
        var bob = await members.CreateAsync(NewMember("b-unarchive@example.com", "Bob"));
        var repo = new InMemoryPrivateMessageRepository(id =>
            members.FindByIdAsync(id).GetAwaiter().GetResult());

        var created = await repo.SendNewOrExistingAsync(
            alice.Id,
            bob.Id,
            "Toggle",
            DateTimeOffset.Parse("2026-08-01T09:20:00Z"));
        var conversationId = created.ConversationId!.Value;

        await repo.ArchiveConversationAsync(conversationId, alice.Id);
        Assert.True(await repo.UnarchiveConversationAsync(conversationId, alice.Id));

        Assert.Single((await repo.GetInboxAsync(alice.Id)).Items);
        Assert.Empty((await repo.GetArchivedInboxAsync(alice.Id)).Items);
    }

    [Fact]
    public async Task Remove_HidesConversationFromInbox_ButNotForOtherParticipant()
    {
        var members = new InMemoryMemberAccountRepository();
        var alice = await members.CreateAsync(NewMember("a-remove@example.com", "Alice"));
        var bob = await members.CreateAsync(NewMember("b-remove@example.com", "Bob"));
        var carol = await members.CreateAsync(NewMember("c-remove@example.com", "Carol"));
        var repo = new InMemoryPrivateMessageRepository(id =>
            members.FindByIdAsync(id).GetAwaiter().GetResult());

        var created = await repo.SendNewOrExistingAsync(
            alice.Id,
            bob.Id,
            "Remove me",
            DateTimeOffset.Parse("2026-08-01T09:00:00Z"));
        var conversationId = created.ConversationId!.Value;

        Assert.True(await repo.RemoveConversationAsync(conversationId, alice.Id));
        Assert.False(await repo.RemoveConversationAsync(conversationId, carol.Id));

        Assert.Empty((await repo.GetInboxAsync(alice.Id)).Items);
        Assert.Single((await repo.GetInboxAsync(bob.Id)).Items);
    }

    [Fact]
    public async Task NewMessage_RestoresRemovedConversation_ForBothParticipants()
    {
        var members = new InMemoryMemberAccountRepository();
        var alice = await members.CreateAsync(NewMember("a-restore@example.com", "Alice"));
        var bob = await members.CreateAsync(NewMember("b-restore@example.com", "Bob"));
        var repo = new InMemoryPrivateMessageRepository(id =>
            members.FindByIdAsync(id).GetAwaiter().GetResult());

        var created = await repo.SendNewOrExistingAsync(
            alice.Id,
            bob.Id,
            "Start",
            DateTimeOffset.Parse("2026-08-01T09:10:00Z"));
        var conversationId = created.ConversationId!.Value;

        await repo.RemoveConversationAsync(conversationId, alice.Id);
        Assert.Empty((await repo.GetInboxAsync(alice.Id)).Items);

        await repo.ReplyAsync(
            conversationId,
            bob.Id,
            "Reopens it",
            DateTimeOffset.Parse("2026-08-01T09:11:00Z"));

        Assert.Single((await repo.GetInboxAsync(alice.Id)).Items);
    }

    private static MemberAccount NewMember(string email, string name) =>
        new()
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = name,
            CreatedAt = DateTime.UtcNow,
        };
}
