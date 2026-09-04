using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using QueenZone.Data;
using QueenZone.Data.Entities;
using QueenZone.Web;

namespace QueenZone.Web.Tests;

public sealed partial class FanPerformanceReportRoutesTests : IClassFixture<QueenZoneWebApplicationFactory>
{
    private const string AdminEmail = "admin@test.local";
    private readonly QueenZoneWebApplicationFactory factory;

    public FanPerformanceReportRoutesTests(QueenZoneWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task MemberApi_CreateReport_IsIdempotent()
    {
        var member = await CreateMemberAsync("fp-report@example.com", "Reporter Fan");
        using var client = CreateMemberClient(member);

        using var first = await client.PostAsJsonAsync(
            FanPerformanceReportApiEndpoints.ReportPath(187),
            new { reason = "This recording is not the submitter's performance." });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var created = await first.Content.ReadFromJsonAsync<FanPerformanceReportCreatedDto>();
        Assert.NotNull(created);
        Assert.False(created!.AlreadyReported);

        using var second = await client.PostAsJsonAsync(
            FanPerformanceReportApiEndpoints.ReportPath(187),
            new { reason = "Still reporting the same track." });
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var again = await second.Content.ReadFromJsonAsync<FanPerformanceReportCreatedDto>();
        Assert.True(again!.AlreadyReported);
        Assert.Equal(created.ReportId, again.ReportId);
    }

    [Fact]
    public async Task Admin_HideAndResolve_SetsDisplayOff()
    {
        var member = await CreateMemberAsync("fp-hide@example.com", "Hide Reporter");
        var reports = factory.Services.GetRequiredService<IFanPerformanceReportRepository>();
        var created = await reports.CreateAsync(new NewFanPerformanceReport(
            176,
            member.Id,
            "Please hide this.",
            "Dear Mr Murdoch",
            "Manu and Zippo"));

        var admin = factory.CreateAdminClient(AdminEmail);
        var detail = await admin.GetStringAsync($"/admin/fan-performance-reports/{created.ReportId}");
        Assert.Contains("Please hide this.", detail);

        var response = await admin.PostAsync(
            $"/admin/fan-performance-reports/{created.ReportId}/hide",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiforgeryToken(detail),
            }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var updated = await reports.GetByIdAsync(created.ReportId!.Value);
        Assert.Equal(FanPerformanceReportStatus.Resolved, updated!.Status);

        using var scope = factory.Services.CreateScope();
        var publicRepo = scope.ServiceProvider.GetRequiredService<IFanPerformanceRepository>();
        var adminRepo = scope.ServiceProvider.GetRequiredService<IAdminFanPerformanceRepository>();
        Assert.Null(await publicRepo.GetByIdAsync(176));
        Assert.False((await adminRepo.GetByIdAsync(176))!.IsVisible);
    }

    [Fact]
    public async Task Admin_Dismiss_LeavesPublishedRowVisible()
    {
        var member = await CreateMemberAsync("fp-dismiss@example.com", "Dismiss Reporter");
        var reports = factory.Services.GetRequiredService<IFanPerformanceReportRepository>();
        var created = await reports.CreateAsync(new NewFanPerformanceReport(
            173,
            member.Id,
            "Not actually a problem.",
            "Hammer to Fall",
            "Sonic Snafu"));

        var admin = factory.CreateAdminClient(AdminEmail);
        var detail = await admin.GetStringAsync($"/admin/fan-performance-reports/{created.ReportId}");
        var response = await admin.PostAsync(
            $"/admin/fan-performance-reports/{created.ReportId}/dismiss",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = ExtractAntiforgeryToken(detail),
            }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var updated = await reports.GetByIdAsync(created.ReportId!.Value);
        Assert.Equal(FanPerformanceReportStatus.Dismissed, updated!.Status);

        using var scope = factory.Services.CreateScope();
        var publicRepo = scope.ServiceProvider.GetRequiredService<IFanPerformanceRepository>();
        Assert.NotNull(await publicRepo.GetByIdAsync(173));
    }

    [Fact]
    public async Task AdminDashboard_ShowsOpenFanPerformanceReportCount()
    {
        var member = await CreateMemberAsync("fp-dash@example.com", "Dash Reporter");
        await factory.Services.GetRequiredService<IFanPerformanceReportRepository>()
            .CreateAsync(new NewFanPerformanceReport(173, member.Id, "Dashboard count", "Hammer to Fall", "Sonic Snafu"));

        var admin = factory.CreateAdminClient(AdminEmail);
        var dashboard = await admin.GetStringAsync("/admin");
        Assert.Contains("href=\"/admin/fan-performance-reports\"", dashboard);
        Assert.Contains("Fan performance reports", dashboard);
    }

    private HttpClient CreateMemberClient(MemberAccount member)
    {
        var token = factory.Services.GetRequiredService<MobileAuthTokenIssuer>()
            .IssueAccessToken(member.Id, member.Email, member.DisplayName);
        var client = factory.CreateAnonymousClient(allowAutoRedirect: false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task<MemberAccount> CreateMemberAsync(string email, string displayName)
    {
        var members = factory.Services.GetRequiredService<IMemberAccountRepository>();
        return await members.CreateAsync(new MemberAccount
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = displayName,
            CreatedAt = DateTime.UtcNow,
        });
    }

    private static string ExtractAntiforgeryToken(string html)
    {
        var match = AntiforgeryRegex().Match(html);
        Assert.True(match.Success, "Antiforgery token was missing.");
        return match.Groups["token"].Value;
    }

    [GeneratedRegex("""name="__RequestVerificationToken"[^>]*value="(?<token>[^"]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex AntiforgeryRegex();
}
