using QueenZone.Data;
using QueenZone.Data.Entities;

namespace QueenZone.Web.Tests;

public sealed class InMemoryPrivateMessageRepositoryTests
{
    [Fact]
    public async Task Inbox_IsOrderedByMostRecent_AndIsolatedPerParticipant()
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
        Assert.Equal(["Carol", "Bob"], aliceInbox.Select(i => i.OtherParticipantDisplayName).ToArray());

        var bobInbox = await repo.GetInboxAsync(bob.Id);
        Assert.Equal(["Alice"], bobInbox.Select(i => i.OtherParticipantDisplayName).ToArray());
        Assert.DoesNotContain(bobInbox, i => i.OtherParticipantId == carol.Id);
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

        var item = Assert.Single(await repo.GetInboxAsync(bob.Id));
        Assert.Equal(PrivateMessageLimits.PreviewLength, item.LastMessagePreview.Length);
        Assert.DoesNotContain('<', item.LastMessagePreview);
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
