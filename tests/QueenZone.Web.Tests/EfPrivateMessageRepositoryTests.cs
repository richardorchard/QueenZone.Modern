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

        var bobView = await repository.GetConversationAsync(result.ConversationId!.Value, bobId);
        var last = Assert.Single(bobView!.Messages);
        await repository.MarkConversationReadAsync(
            result.ConversationId!.Value,
            bobId,
            last.SortKey,
            last.CreatedAt);
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

    [Fact]
    public async Task Reply_KeepsNewestSummary_WhenOlderReplyCommitsLater()
    {
        var created = await repository.SendNewOrExistingAsync(
            aliceId,
            bobId,
            "Start",
            DateTimeOffset.Parse("2026-08-02T12:00:00Z"));
        var conversationId = created.ConversationId!.Value;

        await repository.ReplyAsync(
            conversationId,
            bobId,
            "Newer reply",
            DateTimeOffset.Parse("2026-08-02T12:02:00Z"));
        await repository.ReplyAsync(
            conversationId,
            aliceId,
            "Older reply commits later",
            DateTimeOffset.Parse("2026-08-02T12:01:00Z"));

        var inbox = await repository.GetInboxAsync(aliceId);
        var item = Assert.Single(inbox);
        Assert.Equal("Newer reply", item.LastMessagePreview);
        Assert.Equal(DateTimeOffset.Parse("2026-08-02T12:02:00Z"), item.LastMessageAt);

        var detail = await repository.GetConversationAsync(conversationId, aliceId);
        Assert.Equal(3, detail!.Messages.Count);
    }

    [Fact]
    public async Task ConcurrentFirstSends_ReuseSingleConversation()
    {
        const string shared = "Data Source=file:pm-race?mode=memory&cache=shared";
        await using var keepAlive = new SqliteConnection(shared);
        keepAlive.Open();

        await using (var setup = CreateContext(shared))
        {
            setup.Database.EnsureCreated();
            setup.MemberAccounts.AddRange(
                new MemberAccount
                {
                    Id = aliceId,
                    Email = "race-alice@example.com",
                    NormalizedEmail = "RACE-ALICE@EXAMPLE.COM",
                    DisplayName = "Race Alice",
                    CreatedAt = DateTime.UtcNow,
                },
                new MemberAccount
                {
                    Id = bobId,
                    Email = "race-bob@example.com",
                    NormalizedEmail = "RACE-BOB@EXAMPLE.COM",
                    DisplayName = "Race Bob",
                    CreatedAt = DateTime.UtcNow,
                });
            await setup.SaveChangesAsync();
        }

        async Task<PrivateMessageSendResult> SendAsync(string body, DateTimeOffset sentAt)
        {
            await using var context = CreateContext(shared);
            var repo = new EfPrivateMessageRepository(context);
            return await repo.SendNewOrExistingAsync(aliceId, bobId, body, sentAt);
        }

        var t1 = DateTimeOffset.Parse("2026-08-02T13:00:00Z");
        var t2 = DateTimeOffset.Parse("2026-08-02T13:00:01Z");
        var results = await Task.WhenAll(
            SendAsync("Concurrent A", t1),
            SendAsync("Concurrent B", t2));

        Assert.All(results, r => Assert.True(r.Succeeded, r.ErrorMessage));
        Assert.Equal(results[0].ConversationId, results[1].ConversationId);

        await using var verify = CreateContext(shared);
        var conversations = await verify.PrivateConversations.CountAsync();
        var messageCount = await verify.PrivateMessages.CountAsync();
        Assert.Equal(1, conversations);
        Assert.Equal(2, messageCount);
    }

    [Fact]
    public void IsUniqueConstraintViolation_DetectsSqliteUniqueErrors()
    {
        var sqlite = new Exception("UNIQUE constraint failed: PrivateConversations.MemberLowId, PrivateConversations.MemberHighId");
        var wrapped = new DbUpdateException("conflict", sqlite);
        Assert.True(EfPrivateMessageRepository.IsUniqueConstraintViolation(wrapped));
    }

    [Fact]
    public async Task MarkConversationRead_IsConditionalAcrossContexts()
    {
        var created = await repository.SendNewOrExistingAsync(
            aliceId,
            bobId,
            "One",
            DateTimeOffset.Parse("2026-08-02T14:00:00Z"));
        var conversationId = created.ConversationId!.Value;
        await repository.ReplyAsync(
            conversationId,
            aliceId,
            "Two",
            DateTimeOffset.Parse("2026-08-02T14:01:00Z"));

        var detail = await repository.GetConversationAsync(conversationId, bobId);
        Assert.Equal(2, detail!.Messages.Count);
        var older = detail.Messages[0];
        var newer = detail.Messages[1];

        await repository.MarkConversationReadAsync(conversationId, bobId, newer.SortKey, newer.CreatedAt);
        await repository.MarkConversationReadAsync(conversationId, bobId, older.SortKey, older.CreatedAt);

        Assert.Equal(0, await repository.CountUnreadConversationsAsync(bobId));
    }

    [Fact]
    public async Task UnreadCount_UsesSortKeyAggregate_NotFullHistoryScanSemantics()
    {
        var created = await repository.SendNewOrExistingAsync(
            aliceId,
            bobId,
            "Seed",
            DateTimeOffset.Parse("2026-08-02T15:00:00Z"));
        var conversationId = created.ConversationId!.Value;
        for (var i = 0; i < 25; i++)
        {
            await repository.ReplyAsync(
                conversationId,
                aliceId,
                $"Msg {i}",
                DateTimeOffset.Parse("2026-08-02T15:00:00Z").AddSeconds(i + 1));
        }

        var detail = await repository.GetConversationAsync(conversationId, bobId);
        var midpoint = detail!.Messages[10];
        await repository.MarkConversationReadAsync(
            conversationId,
            bobId,
            midpoint.SortKey,
            midpoint.CreatedAt);

        var inbox = await repository.GetInboxAsync(bobId);
        var item = Assert.Single(inbox);
        Assert.True(item.HasUnread);
        Assert.Equal(detail.Messages.Count(m => !m.IsMine && m.SortKey > midpoint.SortKey), item.UnreadCount);
    }

    private static QueenZoneDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<QueenZoneDbContext>()
            .UseSqlite(connectionString)
            .Options;
        return new QueenZoneDbContext(options);
    }

    public async ValueTask DisposeAsync()
    {
        await dbContext.DisposeAsync();
        await connection.DisposeAsync();
    }
}
