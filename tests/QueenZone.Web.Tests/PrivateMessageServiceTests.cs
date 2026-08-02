using QueenZone.Data;
using QueenZone.Data.Entities;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed class PrivateMessageServiceTests
{
    [Fact]
    public async Task Compose_CreatesConversation_AndRecipientSeesUnread()
    {
        var (service, members, messages, alice, bob) = CreateSystem();

        var result = await service.ComposeAsync(alice.Id, bob.Id, "Hello Bob");
        Assert.True(result.Succeeded);
        Assert.NotNull(result.ConversationId);

        var bobInbox = await service.GetInboxAsync(bob.Id);
        var item = Assert.Single(bobInbox);
        Assert.Equal(alice.Id, item.OtherParticipantId);
        Assert.True(item.HasUnread);
        Assert.Equal(1, item.UnreadCount);
        Assert.Equal("Hello Bob", item.LastMessagePreview);

        Assert.Equal(1, await service.CountUnreadConversationsAsync(bob.Id));
        Assert.Equal(0, await service.CountUnreadConversationsAsync(alice.Id));
    }

    [Fact]
    public async Task Compose_RejectsEmptySelfAndMissingRecipient()
    {
        var (service, _, _, alice, bob) = CreateSystem();

        var empty = await service.ComposeAsync(alice.Id, bob.Id, "   ");
        Assert.False(empty.Succeeded);
        Assert.Contains("required", empty.ErrorMessage, StringComparison.OrdinalIgnoreCase);

        var self = await service.ComposeAsync(alice.Id, alice.Id, "hi");
        Assert.False(self.Succeeded);
        Assert.Contains("yourself", self.ErrorMessage, StringComparison.OrdinalIgnoreCase);

        var missing = await service.ComposeAsync(alice.Id, Guid.NewGuid(), "hi");
        Assert.False(missing.Succeeded);
        Assert.Contains("not found", missing.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Compose_ReusesExistingOneToOneConversation()
    {
        var (service, _, _, alice, bob) = CreateSystem();

        var first = await service.ComposeAsync(alice.Id, bob.Id, "First");
        var second = await service.ComposeAsync(alice.Id, bob.Id, "Second");

        Assert.Equal(first.ConversationId, second.ConversationId);

        var detail = await service.GetConversationAsync(first.ConversationId!.Value, alice.Id, markRead: false);
        Assert.Equal(2, detail!.Messages.Count);
        Assert.Equal("Second", detail.Messages[^1].Body);
    }

    [Fact]
    public async Task Reply_UpdatesUnread_AndOrdersOldestFirst()
    {
        var (service, _, _, alice, bob) = CreateSystem();
        var created = await service.ComposeAsync(alice.Id, bob.Id, "Hello");
        var conversationId = created.ConversationId!.Value;

        await service.GetConversationAsync(conversationId, bob.Id, markRead: true);
        Assert.Equal(0, await service.CountUnreadConversationsAsync(bob.Id));

        var reply = await service.ReplyAsync(conversationId, bob.Id, "Hi Alice");
        Assert.True(reply.Succeeded);

        Assert.Equal(1, await service.CountUnreadConversationsAsync(alice.Id));
        Assert.Equal(0, await service.CountUnreadConversationsAsync(bob.Id));

        var aliceView = await service.GetConversationAsync(conversationId, alice.Id, markRead: true);
        Assert.Equal(["Hello", "Hi Alice"], aliceView!.Messages.Select(m => m.Body).ToArray());
        Assert.Equal(0, await service.CountUnreadConversationsAsync(alice.Id));
    }

    [Fact]
    public async Task Reply_RejectsNonParticipant()
    {
        var (service, members, _, alice, bob) = CreateSystem();
        var carol = await members.CreateAsync(new MemberAccount
        {
            Id = Guid.NewGuid(),
            Email = "carol@example.com",
            DisplayName = "Carol",
            CreatedAt = DateTime.UtcNow,
        });

        var created = await service.ComposeAsync(alice.Id, bob.Id, "Private");
        var result = await service.ReplyAsync(created.ConversationId!.Value, carol.Id, "Intruder");

        Assert.False(result.Succeeded);
        Assert.Contains("not a participant", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetConversation_ReturnsNullForNonParticipant()
    {
        var (service, members, _, alice, bob) = CreateSystem();
        var carol = await members.CreateAsync(new MemberAccount
        {
            Id = Guid.NewGuid(),
            Email = "carol2@example.com",
            DisplayName = "Carol Two",
            CreatedAt = DateTime.UtcNow,
        });

        var created = await service.ComposeAsync(alice.Id, bob.Id, "Secret");
        var detail = await service.GetConversationAsync(created.ConversationId!.Value, carol.Id);
        Assert.Null(detail);
    }

    [Fact]
    public async Task OwnSentMessages_DoNotCreateUnreadForSender()
    {
        var (service, _, _, alice, bob) = CreateSystem();
        await service.ComposeAsync(alice.Id, bob.Id, "Ping");

        var aliceInbox = await service.GetInboxAsync(alice.Id);
        Assert.False(Assert.Single(aliceInbox).HasUnread);
        Assert.Equal(0, await service.CountUnreadConversationsAsync(alice.Id));
    }

    [Fact]
    public async Task SearchRecipients_ExcludesSelf_AndMatchesDisplayName()
    {
        var (service, members, _, alice, bob) = CreateSystem();
        await members.CreateAsync(new MemberAccount
        {
            Id = Guid.NewGuid(),
            Email = "bobby@example.com",
            DisplayName = "Bobby",
            CreatedAt = DateTime.UtcNow,
        });

        var matches = await service.SearchRecipientsAsync(alice.Id, "Bob");
        Assert.Contains(matches, m => m.MemberId == bob.Id);
        Assert.DoesNotContain(matches, m => m.MemberId == alice.Id);
        Assert.Contains(matches, m => m.DisplayName == "Bobby");
    }

    [Theory]
    [InlineData(null, null, false)]
    [InlineData("11111111-1111-1111-1111-111111111111", null, false)]
    [InlineData("11111111-1111-1111-1111-111111111111", "11111111-1111-1111-1111-111111111111", false)]
    [InlineData("11111111-1111-1111-1111-111111111111", "22222222-2222-2222-2222-222222222222", true)]
    public void CanMessage_EnforcesSelfAndPresence(string? current, string? target, bool expected)
    {
        Guid? currentId = current is null ? null : Guid.Parse(current);
        Guid? targetId = target is null ? null : Guid.Parse(target);
        Assert.Equal(expected, PrivateMessageService.CanMessage(currentId, targetId));
    }


    [Fact]
    public async Task GetConversation_MarkRead_UsesLastReturnedMessageCursor()
    {
        var (service, _, messages, alice, bob) = CreateSystem();
        var created = await service.ComposeAsync(alice.Id, bob.Id, "Visible");
        var conversationId = created.ConversationId!.Value;

        // Simulate a message that arrives/commits after the thread was loaded but with a
        // timestamp after the last returned message. It must remain unread.
        var detail = await messages.GetConversationAsync(conversationId, bob.Id);
        Assert.NotNull(detail);
        var lastSeen = detail!.Messages[^1].CreatedAt;

        await messages.ReplyAsync(
            conversationId,
            alice.Id,
            "Arrived during load",
            lastSeen.AddSeconds(5));

        await messages.MarkConversationReadAsync(conversationId, bob.Id, lastSeen);

        Assert.Equal(1, await service.CountUnreadConversationsAsync(bob.Id));
        var inbox = await service.GetInboxAsync(bob.Id);
        Assert.True(Assert.Single(inbox).HasUnread);
    }

    [Fact]
    public async Task GetConversation_MarkRead_DoesNotUseWallClock()
    {
        var (service, _, messages, alice, bob) = CreateSystem();
        var created = await service.ComposeAsync(alice.Id, bob.Id, "Hello");
        var conversationId = created.ConversationId!.Value;

        await service.GetConversationAsync(conversationId, bob.Id, markRead: true);

        // Insert a message timestamped before "now" but after the last seen message.
        // If mark-read used UtcNow, this would incorrectly appear read.
        var detail = await messages.GetConversationAsync(conversationId, bob.Id);
        var lastSeen = detail!.Messages[^1].CreatedAt;
        await messages.ReplyAsync(
            conversationId,
            alice.Id,
            "Between last-seen and now",
            lastSeen.AddMilliseconds(1));

        Assert.Equal(1, await service.CountUnreadConversationsAsync(bob.Id));
    }

    private static (
        PrivateMessageService Service,
        IMemberAccountRepository Members,
        IPrivateMessageRepository Messages,
        MemberAccount Alice,
        MemberAccount Bob) CreateSystem()
    {
        var members = new InMemoryMemberAccountRepository();
        var alice = members.CreateAsync(new MemberAccount
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Email = "alice@example.com",
            DisplayName = "Alice",
            CreatedAt = DateTime.UtcNow,
        }).GetAwaiter().GetResult();
        var bob = members.CreateAsync(new MemberAccount
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Email = "bob@example.com",
            DisplayName = "Bob",
            CreatedAt = DateTime.UtcNow,
        }).GetAwaiter().GetResult();

        var messages = new InMemoryPrivateMessageRepository(id =>
            members.FindByIdAsync(id).GetAwaiter().GetResult());
        var service = new PrivateMessageService(messages, members, TimeProvider.System);
        return (service, members, messages, alice, bob);
    }
}
