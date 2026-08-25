using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QueenZone.Data;
using QueenZone.Data.Entities;

namespace QueenZone.Web.Tests;

public sealed class EfPrivateMessageReportReviewRepositoryTests : IAsyncDisposable
{
    private readonly SqliteConnection connection;
    private readonly QueenZoneDbContext dbContext;
    private readonly EfPrivateMessageRepository messages;
    private readonly EfPrivateMessageReportReviewRepository reviews;
    private readonly Guid aliceId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa01");
    private readonly Guid bobId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbb01");
    private readonly Guid carolId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccc01");

    public EfPrivateMessageReportReviewRepositoryTests()
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
                Email = "alice-review-ef@example.com",
                NormalizedEmail = "ALICE-REVIEW-EF@EXAMPLE.COM",
                DisplayName = "Alice EF",
                CreatedAt = DateTime.UtcNow,
            },
            new MemberAccount
            {
                Id = bobId,
                Email = "bob-review-ef@example.com",
                NormalizedEmail = "BOB-REVIEW-EF@EXAMPLE.COM",
                DisplayName = "Bob EF",
                CreatedAt = DateTime.UtcNow,
            },
            new MemberAccount
            {
                Id = carolId,
                Email = "carol-review-ef@example.com",
                NormalizedEmail = "CAROL-REVIEW-EF@EXAMPLE.COM",
                DisplayName = "Carol EF",
                CreatedAt = DateTime.UtcNow,
            });
        dbContext.SaveChanges();

        messages = new EfPrivateMessageRepository(dbContext);
        reviews = new EfPrivateMessageReportReviewRepository(dbContext);
    }

    [Fact]
    public async Task GetReportedMessageContext_ReturnsSnapshotAndIdentities_WithoutOtherThreads()
    {
        var first = await messages.SendNewOrExistingAsync(aliceId, bobId, "Context", DateTimeOffset.UtcNow);
        var conversationId = first.ConversationId!.Value;
        await messages.ReplyAsync(conversationId, aliceId, "Reported EF body", DateTimeOffset.UtcNow);
        var target = (await messages.GetConversationAsync(conversationId, bobId))!.Messages[^1];
        await messages.SendNewOrExistingAsync(aliceId, carolId, "Unrelated thread", DateTimeOffset.UtcNow);

        var created = await messages.CreateReportAsync(
            bobId,
            conversationId,
            target.Id,
            "Abuse",
            DateTimeOffset.UtcNow);
        Assert.True(created.Succeeded);

        var context = await reviews.GetReportedMessageContextAsync(created.ReportId!.Value);
        Assert.NotNull(context);
        Assert.Equal("Reported EF body", context!.Report.MessageBodySnapshot);
        Assert.Equal("Context", Assert.Single(context.Report.PrecedingMessages).Body);
        Assert.Equal("Bob EF", context.ReporterDisplayName);
        Assert.Equal("Alice EF", context.ReportedDisplayName);
        Assert.DoesNotContain("Unrelated thread", context.Report.MessageBodySnapshot);

        var listed = await reviews.ListReportsAsync(PrivateMessageReportStatus.Open, 1, 50);
        Assert.Equal(created.ReportId, Assert.Single(listed.Items).Id);
        Assert.Equal(1, await reviews.CountOpenAsync());
    }

    [Fact]
    public async Task UpdateStatus_WritesAudit_AndUnknownReportIsNull()
    {
        var sent = await messages.SendNewOrExistingAsync(aliceId, bobId, "Hi EF", DateTimeOffset.UtcNow);
        var message = (await messages.GetConversationAsync(sent.ConversationId!.Value, bobId))!.Messages[0];
        var created = await messages.CreateReportAsync(
            bobId,
            sent.ConversationId.Value,
            message.Id,
            "Reason",
            DateTimeOffset.UtcNow);

        Assert.True(await reviews.RecordAccessAsync(
            created.ReportId!.Value,
            PrivateMessageReportAuditActions.Viewed,
            "admin@example.com",
            null));
        var updated = await reviews.UpdateReportStatusAsync(
            created.ReportId.Value,
            PrivateMessageReportStatus.Reviewed,
            "admin@example.com",
            "Looking into it");
        Assert.Equal(PrivateMessageReportStatus.Reviewed, updated!.Status);

        var context = await reviews.GetReportedMessageContextAsync(created.ReportId.Value);
        Assert.Equal("Looking into it", context!.ReviewNotes);
        Assert.Contains(context.AuditLogs, log => log.Action == PrivateMessageReportAuditActions.Viewed);
        Assert.Contains(context.AuditLogs, log => log.Action == PrivateMessageReportAuditActions.StatusChanged);
        Assert.Equal(0, await reviews.CountOpenAsync());
        Assert.Null(await reviews.GetReportedMessageContextAsync(Guid.NewGuid()));
        Assert.Null(await reviews.UpdateReportStatusAsync(
            Guid.NewGuid(),
            PrivateMessageReportStatus.Dismissed,
            "admin@example.com",
            null));
    }

    public ValueTask DisposeAsync()
    {
        dbContext.Dispose();
        connection.Dispose();
        return ValueTask.CompletedTask;
    }
}
