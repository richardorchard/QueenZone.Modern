using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using QueenZone.Data;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed partial class AdminHelpRequestRoutesTests : IClassFixture<QueenZoneWebApplicationFactory>
{
    private const string AdminEmail = "admin@test.local";
    private readonly QueenZoneWebApplicationFactory factory;

    public AdminHelpRequestRoutesTests(QueenZoneWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Get_AdminHelp_RequiresAdminAuthentication()
    {
        var anonymous = factory.CreateAnonymousClient(allowAutoRedirect: false);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/admin/help")).StatusCode);

        var stranger = factory.CreateAdminClient("stranger@example.com");
        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.GetAsync("/admin/help")).StatusCode);

        var admin = factory.CreateAdminClient(AdminEmail);
        var body = await admin.GetStringAsync("/admin/help");
        Assert.Contains("Help requests", body);
    }

    [Fact]
    public async Task AdminDashboard_ShowsHelpSectionAndOpenCount()
    {
        var repository = factory.Services.GetRequiredService<IHelpRequestRepository>();
        await repository.CreateAsync(Sample("Dashboard visible request"));

        var admin = factory.CreateAdminClient(AdminEmail);
        var dashboard = await admin.GetStringAsync("/admin");

        Assert.Contains("href=\"/admin/help\"", dashboard);
        Assert.Contains("Help requests", dashboard);
        Assert.Contains("open or in progress", dashboard);
    }

    [Fact]
    public async Task Admin_CanOpenDetailAndChangeStatus()
    {
        var repository = factory.Services.GetRequiredService<IHelpRequestRepository>();
        var created = await repository.CreateAsync(Sample("Please look at this account problem"));

        var admin = factory.CreateAdminClient(AdminEmail);
        var queue = await admin.GetStringAsync("/admin/help");
        Assert.Contains("Please look at this account problem", queue);
        Assert.Contains($"/admin/help/{created.Id}", queue);

        var detail = await admin.GetStringAsync($"/admin/help/{created.Id}");
        Assert.Contains("Please look at this account problem", detail);
        Assert.Contains("guest-help@example.com", detail);
        Assert.Contains("This is a sufficiently long help message", detail);

        var response = await admin.PostAsync(
            $"/admin/help/{created.Id}/status",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiforgeryToken(detail),
                ["status"] = HelpRequestStatus.Resolved,
                ["reviewNotes"] = "Replied by email",
            }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal($"/admin/help/{created.Id}", response.Headers.Location!.OriginalString);

        var updated = await repository.GetByIdAsync(created.Id);
        Assert.NotNull(updated);
        Assert.Equal(HelpRequestStatus.Resolved, updated!.Status);
        Assert.Equal("Replied by email", updated.ReviewNotes);
        Assert.Equal(AdminEmail, updated.ReviewerEmail);

        var resolvedQueue = await admin.GetStringAsync("/admin/help?status=Resolved");
        Assert.Contains("Please look at this account problem", resolvedQueue);
    }

    [Fact]
    public async Task Admin_UnknownRequest_ReturnsNotFound()
    {
        var admin = factory.CreateAdminClient(AdminEmail);
        var response = await admin.GetAsync($"/admin/help/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static HelpRequest Sample(string subject) =>
        new(
            Guid.NewGuid(),
            HelpRequestTopic.Account,
            subject,
            "This is a sufficiently long help message for the admin inbox.",
            "Guest User",
            "guest-help@example.com",
            "GUEST-HELP@EXAMPLE.COM",
            null,
            HelpRequestStatus.Open,
            DateTimeOffset.UtcNow,
            null,
            null,
            null);

    private static string ExtractAntiforgeryToken(string html)
    {
        var match = AntiforgeryTokenRegex().Match(html);
        Assert.True(match.Success, "Antiforgery token was not found in the form.");
        return match.Groups["token"].Value;
    }

    [GeneratedRegex("""name="__RequestVerificationToken"[^>]*value="(?<token>[^"]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex AntiforgeryTokenRegex();
}
