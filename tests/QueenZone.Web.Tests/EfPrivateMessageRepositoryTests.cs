using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QueenZone.Data;
using QueenZone.Data.Entities;

namespace QueenZone.Web.Tests;

public sealed class EfPrivateMessageRepositoryTests : IAsyncDisposable
{
    private readonly SqliteConnection connection;
    private readonly QueenZoneDbContext dbContext;
    private readonly EfPrivateMessageRepository repository;
    private readonly Guid aliceId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private readonly Guid bobId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private readonly Guid carolId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    public EfPrivateMessageRepositoryTests()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<QueenZoneDbContext>()
            .UseSqlite(connection)
            .Options;
        dbContext = new QueenZoneDbContext(options);
        dbContext.Database.EnsureCreated();

        dbContext.MemberAccounts.AddRange(
            new MemberAccount
            {
                Id = aliceId,
                Email = "alice-ef@example.com",
                NormalizedEmail = "ALICE-EF@EXAMPLE.COM",
                DisplayName = "Alice EF",
                CreatedAt = DateTime.UtcNow,
            },
            new MemberAccount
            {
                Id = bobId,
                Email = "bob-ef@example.com",
                NormalizedEmail = "BOB-EF@EXAMPLE.COM",
                DisplayName = "Bob EF",
                CreatedAt = DateTime.UtcNow,
            },
            new MemberAccount
            {
                Id = carolId,
                Email = "carol-ef@example.com",
                NormalizedEmail = "CAROL-EF@EXAMPLE.COM",
                DisplayName = "Carol EF",
                CreatedAt = DateTime.UtcNow,
            });
        dbContext.SaveChanges();

        repository = new EfPrivateMessageRepository(dbContext);
    }

    [Fact]
    public async Task Send_And_GetConversation_RoundTrip()
    {
        var sentAt = DateTimeOffset.Parse("2026-08-02T10:00:00Z");
        var result = await repository.SendNewOrExistingAsync(aliceId, bobId, "EF hello", sentAt);
        Assert.True(result.Succeeded);

        var detail = await repository.GetConversationAsync(result.ConversationId!.Value, bobId);
        Assert.NotNull(detail);
        Assert.Equal("Alice EF", detail!.OtherParticipantDisplayName);
        Assert.Equal("EF hello", Assert.Single(detail.Messages).Body);

        Assert.True(await repository.IsParticipantAsync(result.ConversationId.Value, aliceId));
        Assert.False(await repository.IsParticipantAsync(result.ConversationId.Value, carolId));
    }

    [Fact]
    public async Task UnreadCount_AndMarkRead_ArePerParticipant()
    {
        var result = await repository.SendNewOrExistingAsync(
            aliceId,
            bobId,
            "Unread me",
            DateTimeOffset.Parse("2026-08-02T11:00:00Z"));

        Assert.Equal(1, await repository.CountUnreadConversationsAsync(bobId));
        Assert.Equal(0, await repository.CountUnreadConversationsAsync(aliceId));

        await repository.MarkConversationReadAsync(
            result.ConversationId!.Value,
            bobId,
            DateTimeOffset.Parse("2026-08-02T11:05:00Z"));
        Assert.Equal(0, await repository.CountUnreadConversationsAsync(bobId));

        await repository.ReplyAsync(
            result.ConversationId.Value,
            bobId,
            "Reply",
            DateTimeOffset.Parse("2026-08-02T11:10:00Z"));
        Assert.Equal(1, await repository.CountUnreadConversationsAsync(aliceId));
    }

    [Fact]
    public async Task Inbox_OrdersByLastMessage_AndHidesOtherConversations()
    {
        await repository.SendNewOrExistingAsync(
            aliceId,
            bobId,
            "Older",
            DateTimeOffset.Parse("2026-08-02T08:00:00Z"));
        await repository.SendNewOrExistingAsync(
            aliceId,
            carolId,
            "Newer",
            DateTimeOffset.Parse("2026-08-02T09:00:00Z"));

        var inbox = await repository.GetInboxAsync(aliceId);
        Assert.Equal(["Carol EF", "Bob EF"], inbox.Select(i => i.OtherParticipantDisplayName).ToArray());

        var bobInbox = await repository.GetInboxAsync(bobId);
        Assert.DoesNotContain(bobInbox, i => i.OtherParticipantId == carolId);
    }

    [Fact]
    public async Task Reply_RejectsNonParticipant()
    {
        var created = await repository.SendNewOrExistingAsync(
            aliceId,
            bobId,
            "Private",
            DateTimeOffset.UtcNow);
        var reply = await repository.ReplyAsync(
            created.ConversationId!.Value,
            carolId,
            "Nope",
            DateTimeOffset.UtcNow);
        Assert.False(reply.Succeeded);
    }

    public async ValueTask DisposeAsync()
    {
        await dbContext.DisposeAsync();
        await connection.DisposeAsync();
    }
}
