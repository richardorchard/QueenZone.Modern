using QueenZone.Data;
using QueenZone.Data.Entities;

namespace QueenZone.Web.Tests;

public sealed class InMemoryPrivateMessageReportReviewRepositoryTests
{
    [Fact]
    public async Task ReviewContext_UsesSnapshot_AndDoesNotBrowseOtherConversations()
    {
        var (members, repo) = CreateRepo();
        var alice = await members.CreateAsync(NewMember("alice-review@example.com", "Alice"));
        var bob = await members.CreateAsync(NewMember("bob-review@example.com", "Bob"));
        var carol = await members.CreateAsync(NewMember("carol-review@example.com", "Carol"));

        var reported = await repo.SendNewOrExistingAsync(alice.Id, bob.Id, "Before", DateTimeOffset.UtcNow);
        var conversationId = reported.ConversationId!.Value;
        await repo.ReplyAsync(conversationId, alice.Id, "Reported body", DateTimeOffset.UtcNow);
        var target = (await repo.GetConversationAsync(conversationId, bob.Id))!.Messages[^1];
        await repo.SendNewOrExistingAsync(alice.Id, carol.Id, "Secret other thread", DateTimeOffset.UtcNow);

        var created = await repo.CreateReportAsync(
            bob.Id,
            conversationId,
            target.Id,
            "Abuse",
            DateTimeOffset.UtcNow);
        Assert.True(created.Succeeded);

        IPrivateMessageReportReviewRepository reviews = repo;
        var context = await reviews.GetReportedMessageContextAsync(created.ReportId!.Value);
        Assert.NotNull(context);
        Assert.Equal("Reported body", context!.Report.MessageBodySnapshot);
        Assert.Equal("Before", Assert.Single(context.Report.PrecedingMessages).Body);
        Assert.Equal("Bob", context.ReporterDisplayName);
        Assert.Equal("Alice", context.ReportedDisplayName);
        Assert.DoesNotContain(
            context.Report.PrecedingMessages,
            item => item.Body.Contains("Secret other thread", StringComparison.Ordinal));
        Assert.DoesNotContain("Secret other thread", context.Report.MessageBodySnapshot);

        var listed = await reviews.ListReportsAsync(PrivateMessageReportStatus.Open, 1, 50);
        var item = Assert.Single(listed.Items);
        Assert.Equal(created.ReportId, item.Id);
        Assert.Equal("Bob", item.ReporterDisplayName);
        Assert.Equal("Alice", item.ReportedDisplayName);
        Assert.Equal(1, await reviews.CountOpenAsync());
    }

    [Fact]
    public async Task UpdateStatus_AndRecordAccess_WriteAuditLog()
    {
        var (members, repo) = CreateRepo();
        var alice = await members.CreateAsync(NewMember("alice-audit@example.com", "Alice"));
        var bob = await members.CreateAsync(NewMember("bob-audit@example.com", "Bob"));
        var sent = await repo.SendNewOrExistingAsync(alice.Id, bob.Id, "Hi", DateTimeOffset.UtcNow);
        var message = (await repo.GetConversationAsync(sent.ConversationId!.Value, bob.Id))!.Messages[0];
        var created = await repo.CreateReportAsync(
            bob.Id,
            sent.ConversationId.Value,
            message.Id,
            null,
            DateTimeOffset.UtcNow);

        IPrivateMessageReportReviewRepository reviews = repo;
        Assert.True(await reviews.RecordAccessAsync(
            created.ReportId!.Value,
            PrivateMessageReportAuditActions.Viewed,
            "mod@example.com",
            null));
        var updated = await reviews.UpdateReportStatusAsync(
            created.ReportId.Value,
            PrivateMessageReportStatus.Dismissed,
            "mod@example.com",
            "Not abuse");
        Assert.Equal(PrivateMessageReportStatus.Dismissed, updated!.Status);

        var context = await reviews.GetReportedMessageContextAsync(created.ReportId.Value);
        Assert.Equal("Not abuse", context!.ReviewNotes);
        Assert.Equal("mod@example.com", context.ReviewerEmail);
        Assert.Contains(context.AuditLogs, log => log.Action == PrivateMessageReportAuditActions.Viewed);
        Assert.Contains(
            context.AuditLogs,
            log => log.Action == PrivateMessageReportAuditActions.StatusChanged
                && log.Details == $"{PrivateMessageReportStatus.Open} → {PrivateMessageReportStatus.Dismissed}");
        Assert.Equal(0, await reviews.CountOpenAsync());
        Assert.Null(await reviews.GetReportedMessageContextAsync(Guid.NewGuid()));
        Assert.False(await reviews.RecordAccessAsync(
            Guid.NewGuid(),
            PrivateMessageReportAuditActions.Viewed,
            "mod@example.com",
            null));
    }

    [Fact]
    public async Task ListReports_FiltersByStatus()
    {
        var (members, repo) = CreateRepo();
        var alice = await members.CreateAsync(NewMember("alice-filter@example.com", "Alice"));
        var bob = await members.CreateAsync(NewMember("bob-filter@example.com", "Bob"));
        var first = await repo.SendNewOrExistingAsync(alice.Id, bob.Id, "One", DateTimeOffset.UtcNow);
        var firstMessage = (await repo.GetConversationAsync(first.ConversationId!.Value, bob.Id))!.Messages[0];
        var open = await repo.CreateReportAsync(
            bob.Id,
            first.ConversationId.Value,
            firstMessage.Id,
            "Open one",
            DateTimeOffset.UtcNow);

        var carol = await members.CreateAsync(NewMember("carol-filter@example.com", "Carol"));
        var second = await repo.SendNewOrExistingAsync(alice.Id, carol.Id, "Two", DateTimeOffset.UtcNow);
        var secondMessage = (await repo.GetConversationAsync(second.ConversationId!.Value, carol.Id))!.Messages[0];
        var dismissed = await repo.CreateReportAsync(
            carol.Id,
            second.ConversationId.Value,
            secondMessage.Id,
            "Dismiss this",
            DateTimeOffset.UtcNow);

        IPrivateMessageReportReviewRepository reviews = repo;
        await reviews.UpdateReportStatusAsync(
            dismissed.ReportId!.Value,
            PrivateMessageReportStatus.Dismissed,
            "mod@example.com",
            null);

        var openPage = await reviews.ListReportsAsync(PrivateMessageReportStatus.Open, 1, 50);
        Assert.Equal(open.ReportId, Assert.Single(openPage.Items).Id);

        var dismissedPage = await reviews.ListReportsAsync(PrivateMessageReportStatus.Dismissed, 1, 50);
        Assert.Equal(dismissed.ReportId, Assert.Single(dismissedPage.Items).Id);

        var all = await reviews.ListReportsAsync("all", 1, 50);
        Assert.Equal(2, all.Items.Count);
        Assert.Null(all.StatusFilter);
    }

    private static (InMemoryMemberAccountRepository Members, InMemoryPrivateMessageRepository Repo) CreateRepo()
    {
        var members = new InMemoryMemberAccountRepository();
        var repo = new InMemoryPrivateMessageRepository(id =>
            members.FindByIdAsync(id).GetAwaiter().GetResult());
        return (members, repo);
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
