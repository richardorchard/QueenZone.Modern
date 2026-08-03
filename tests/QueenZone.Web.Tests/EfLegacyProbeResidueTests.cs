using Microsoft.EntityFrameworkCore;
using QueenZone.Data;

namespace QueenZone.Web.Tests;

[Collection(LiveDatabaseProbeCollection.Name)]
public sealed class EfLegacyProbeResidueTests
{
    [Fact]
    public async Task Known_probe_and_web_test_markers_are_absent_when_check_enabled()
    {
        if (!IsCheckEnabled(out var connectionString))
        {
            return;
        }

        var options = new DbContextOptionsBuilder<QueenZoneDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        await using var dbContext = new QueenZoneDbContext(options);

        Assert.False(await dbContext.NewsRows.AnyAsync(row =>
            (row.Slug != null
                && (row.Slug.StartsWith("probe-write-")
                    || row.Slug.StartsWith("news-section-live-probe-")
                    || row.Slug.StartsWith("full-lifecycle-live-probe-")))
            || row.EditorEmail == "legacy-write-probe@queenzone.local"));
        Assert.False(await dbContext.NewsAuditLogs.AnyAsync(audit =>
            audit.ActorEmail == "legacy-write-probe@queenzone.local"));
        Assert.False(await dbContext.NewsAgentRunRequests.AnyAsync(request =>
            request.RequestedBy == "url-ingestion-probe@queenzone.local"
            || request.RequestedBy == "url-ingestion-full-probe@queenzone.local"
            || (request.ArticleUrl != null && request.ArticleUrl.Contains("qz-url-ingestion-probe"))));
        Assert.False(await dbContext.NewsAgentRunnerHeartbeats.AnyAsync(heartbeat =>
            heartbeat.RunnerId.Contains("url-ingestion")
            && heartbeat.RunnerId.Contains("probe")));
        Assert.False(await dbContext.NewsCandidates.AnyAsync(candidate =>
            candidate.SourceUrl.Contains("qz-url-ingestion-probe")
            || candidate.CanonicalUrl.Contains("qz-url-ingestion-probe")));
        Assert.False(await dbContext.MemberAccounts.AnyAsync(member =>
            member.Email.EndsWith("@example.com")
            || member.Email.EndsWith("@example.test")
            || member.Email.EndsWith("@test.local")));
        Assert.False(await dbContext.PhotoAdminAuditLogs.AnyAsync(audit =>
            audit.ActorEmail == "admin@test.local"));

        var legacyTestPhotoCount = await dbContext.Database
            .SqlQueryRaw<int>(
                """
                SELECT CAST(COUNT(*) AS int) AS [Value]
                FROM dbo.PIC_FILES_T
                WHERE Name IN ('Thumb regen photo', 'Route upload photo')
                """)
            .SingleAsync();
        Assert.Equal(0, legacyTestPhotoCount);
    }

    private static bool IsCheckEnabled(out string connectionString)
    {
        connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__QueenZoneLegacy") ?? string.Empty;
        return !string.IsNullOrWhiteSpace(connectionString)
            && string.Equals(
                Environment.GetEnvironmentVariable("RUN_LEGACY_PROBE_RESIDUE_CHECK"),
                "true",
                StringComparison.OrdinalIgnoreCase);
    }
}
