using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using QueenZone.Data;
using QueenZone.Data.Entities;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed partial class AdminMessageReportRoutesTests : IClassFixture<QueenZoneWebApplicationFactory>
{
    private const string AdminEmail = "admin@test.local";
    private readonly QueenZoneWebApplicationFactory factory;

    public AdminMessageReportRoutesTests(QueenZoneWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Get_AdminMessageReports_RequiresAdminAuthentication()
    {
        var anonymous = factory.CreateAnonymousClient(allowAutoRedirect: false);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/admin/message-reports")).StatusCode);

        var stranger = factory.CreateAdminClient("stranger@example.com");
        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.GetAsync("/admin/message-reports")).StatusCode);

        var admin = factory.CreateAdminClient(AdminEmail);
        var body = await admin.GetStringAsync("/admin/message-reports");
        Assert.Contains("Message reports", body);
        Assert.Contains("no search of private conversations", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AdminDashboard_ShowsMessageReportSectionAndOpenCount()
    {
        await SeedReportAsync("dashboard-alice@example.com", "dashboard-bob@example.com", "Dashboard reported body");

        var admin = factory.CreateAdminClient(AdminEmail);
        var dashboard = await admin.GetStringAsync("/admin");

        Assert.Contains("href=\"/admin/message-reports\"", dashboard);
        Assert.Contains("Message reports", dashboard);
        Assert.Contains("open reports", dashboard);
    }

    [Fact]
    public async Task Admin_CanOpenDetail_SeeSnapshotAndIdentities_AndChangeStatus()
    {
        var report = await SeedReportAsync(
            "review-alice@example.com",
            "review-bob@example.com",
            "Please leave me alone",
            precedingBody: "Earlier hello",
            reason: "Harassment");

        var admin = factory.CreateAdminClient(AdminEmail);
        var queue = await admin.GetStringAsync("/admin/message-reports");
        Assert.Contains("Review Alice", queue);
        Assert.Contains("Review Bob", queue);
        Assert.Contains("Harassment", queue);
        Assert.DoesNotContain("Please leave me alone", queue);
        Assert.Contains($"/admin/message-reports/{report.Id}", queue);

        var detail = await admin.GetStringAsync($"/admin/message-reports/{report.Id}");
        Assert.Contains("Please leave me alone", detail);
        Assert.Contains("Earlier hello", detail);
        Assert.Contains("Review Alice", detail);
        Assert.Contains("Review Bob", detail);
        Assert.Contains($"/admin/members/{report.ReporterMemberId}", detail);
        Assert.Contains($"/admin/members/{report.ReportedMemberId}", detail);
        Assert.Contains("Viewed", detail);
        Assert.Contains(AdminEmail, detail);
        Assert.DoesNotContain("/messages/", detail);

        var response = await admin.PostAsync(
            $"/admin/message-reports/{report.Id}/status",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiforgeryToken(detail),
                ["status"] = PrivateMessageReportStatus.Actioned,
                ["reviewNotes"] = "Warned the sender",
            }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal($"/admin/message-reports/{report.Id}", response.Headers.Location!.OriginalString);

        var reviews = factory.Services.GetRequiredService<IPrivateMessageReportReviewRepository>();
        var updated = await reviews.GetReportedMessageContextAsync(report.Id);
        Assert.NotNull(updated);
        Assert.Equal(PrivateMessageReportStatus.Actioned, updated!.Report.Status);
        Assert.Equal("Warned the sender", updated.ReviewNotes);
        Assert.Equal(AdminEmail, updated.ReviewerEmail);
        Assert.Contains(
            updated.AuditLogs,
            log => log.Action == PrivateMessageReportAuditActions.Viewed && log.ActorEmail == AdminEmail);
        Assert.Contains(
            updated.AuditLogs,
            log => log.Action == PrivateMessageReportAuditActions.StatusChanged
                && log.Details == $"{PrivateMessageReportStatus.Open} → {PrivateMessageReportStatus.Actioned}");

        var actionedQueue = await admin.GetStringAsync("/admin/message-reports?status=Actioned");
        Assert.Contains("Review Alice", actionedQueue);
    }

    [Fact]
    public async Task Admin_UnknownReport_ReturnsNotFound()
    {
        var admin = factory.CreateAdminClient(AdminEmail);
        var response = await admin.GetAsync($"/admin/message-reports/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Admin_HasNoGlobalPrivateMessageBrowseRoute()
    {
        var admin = factory.CreateAdminClient(AdminEmail);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync("/admin/messages")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync("/admin/private-messages")).StatusCode);
    }

    [Fact]
    public void ReviewRepository_OnlyExposesReportScopedLookups()
    {
        var methods = typeof(IPrivateMessageReportReviewRepository).GetMethods();
        Assert.Contains(methods, method => method.Name == nameof(IPrivateMessageReportReviewRepository.GetReportedMessageContextAsync));

        foreach (var method in methods)
        {
            var names = method.GetParameters().Select(parameter => parameter.Name).ToArray();
            Assert.DoesNotContain("conversationId", names);
            Assert.DoesNotContain("memberId", names);
            Assert.DoesNotContain("query", names);
            Assert.DoesNotContain("search", names);
        }

        var context = typeof(IPrivateMessageReportReviewRepository)
            .GetMethod(nameof(IPrivateMessageReportReviewRepository.GetReportedMessageContextAsync));
        Assert.NotNull(context);
        Assert.Equal("reportId", context!.GetParameters()[0].Name);
        Assert.Equal(typeof(Guid), context.GetParameters()[0].ParameterType);
    }

    private async Task<PrivateMessageReport> SeedReportAsync(
        string reporterEmail,
        string reportedEmail,
        string body,
        string? precedingBody = null,
        string? reason = "Abuse")
    {
        var members = factory.Services.GetRequiredService<IMemberAccountRepository>();
        var messages = factory.Services.GetRequiredService<IPrivateMessageRepository>();

        var reporter = await members.CreateAsync(NewMember(reporterEmail, DisplayNameFromEmail(reporterEmail)));
        var reported = await members.CreateAsync(NewMember(reportedEmail, DisplayNameFromEmail(reportedEmail)));
        var sent = await messages.SendNewOrExistingAsync(
            reported.Id,
            reporter.Id,
            precedingBody ?? "Intro",
            DateTimeOffset.UtcNow);
        var conversationId = sent.ConversationId!.Value;
        await messages.ReplyAsync(conversationId, reported.Id, body, DateTimeOffset.UtcNow);
        var target = (await messages.GetConversationAsync(conversationId, reporter.Id))!.Messages[^1];
        var created = await messages.CreateReportAsync(
            reporter.Id,
            conversationId,
            target.Id,
            reason,
            DateTimeOffset.UtcNow);
        Assert.True(created.Succeeded);
        return (await messages.GetReportAsync(created.ReportId!.Value))!;
    }

    private static MemberAccount NewMember(string email, string name) =>
        new()
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = name,
            CreatedAt = DateTime.UtcNow,
        };

    private static string DisplayNameFromEmail(string email)
    {
        var local = email.Split('@')[0];
        var parts = local.Split('-', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(
            ' ',
            parts.Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
    }

    private static string ExtractAntiforgeryToken(string html)
    {
        var match = AntiforgeryTokenRegex().Match(html);
        Assert.True(match.Success, "Antiforgery token was not found in the form.");
        return match.Groups["token"].Value;
    }

    [GeneratedRegex("""name="__RequestVerificationToken"[^>]*value="(?<token>[^"]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex AntiforgeryTokenRegex();
}
