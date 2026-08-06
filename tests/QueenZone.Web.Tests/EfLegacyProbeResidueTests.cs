using Microsoft.EntityFrameworkCore;
using QueenZone.Data;
using QueenZone.Data.Entities;

namespace QueenZone.Web.Tests;

[Collection(LiveDatabaseProbeCollection.Name)]
public sealed class EfLegacyProbeResidueTests
{
    /// <summary>
    /// Deterministic: forces SQL Server translation of every residue marker predicate
    /// without connecting. Catches StringComparison overloads and other non-translatable
    /// LINQ that would only fail on the nightly SQL Express residue step.
    /// </summary>
    [Fact]
    public void Residue_marker_predicates_translate_for_sql_server()
    {
        using var dbContext = CreateSqlServerContext("Server=.;Database=unused;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=1");

        Assert.False(string.IsNullOrWhiteSpace(NewsResidueQuery(dbContext).ToQueryString()));
        Assert.False(string.IsNullOrWhiteSpace(NewsAuditResidueQuery(dbContext).ToQueryString()));
        Assert.False(string.IsNullOrWhiteSpace(RunRequestResidueQuery(dbContext).ToQueryString()));
        Assert.False(string.IsNullOrWhiteSpace(HeartbeatResidueQuery(dbContext).ToQueryString()));
        Assert.False(string.IsNullOrWhiteSpace(CandidateResidueQuery(dbContext).ToQueryString()));
        Assert.False(string.IsNullOrWhiteSpace(DiscoverySourceResidueQuery(dbContext).ToQueryString()));
        Assert.False(string.IsNullOrWhiteSpace(MemberResidueQuery(dbContext).ToQueryString()));
        Assert.False(string.IsNullOrWhiteSpace(PrivateMessageResidueQuery(dbContext).ToQueryString()));
        Assert.False(string.IsNullOrWhiteSpace(ForumThreadResidueQuery(dbContext).ToQueryString()));
        Assert.False(string.IsNullOrWhiteSpace(PhotoSubmissionResidueQuery(dbContext).ToQueryString()));
        Assert.False(string.IsNullOrWhiteSpace(ArticleSubmissionResidueQuery(dbContext).ToQueryString()));
        Assert.False(string.IsNullOrWhiteSpace(NewsSuggestionResidueQuery(dbContext).ToQueryString()));
        Assert.False(string.IsNullOrWhiteSpace(PhotoAdminAuditResidueQuery(dbContext).ToQueryString()));
    }

    [Fact]
    public async Task Known_probe_and_web_test_markers_are_absent_when_check_enabled()
    {
        if (!IsCheckEnabled(out var connectionString))
        {
            return;
        }

        await using var dbContext = CreateSqlServerContext(connectionString);

        Assert.False(await NewsResidueQuery(dbContext).AnyAsync());
        Assert.False(await NewsAuditResidueQuery(dbContext).AnyAsync());
        Assert.False(await RunRequestResidueQuery(dbContext).AnyAsync());
        Assert.False(await HeartbeatResidueQuery(dbContext).AnyAsync());
        Assert.False(await CandidateResidueQuery(dbContext).AnyAsync());
        Assert.False(await DiscoverySourceResidueQuery(dbContext).AnyAsync());
        Assert.False(await MemberResidueQuery(dbContext).AnyAsync());
        Assert.False(await PrivateMessageResidueQuery(dbContext).AnyAsync());
        Assert.False(await ForumThreadResidueQuery(dbContext).AnyAsync());
        Assert.False(await PhotoSubmissionResidueQuery(dbContext).AnyAsync());
        Assert.False(await ArticleSubmissionResidueQuery(dbContext).AnyAsync());
        Assert.False(await NewsSuggestionResidueQuery(dbContext).AnyAsync());
        Assert.False(await PhotoAdminAuditResidueQuery(dbContext).AnyAsync());

        var legacyTestPhotoCount = await dbContext.Database
            .SqlQueryRaw<int>(
                """
                SELECT CAST(COUNT(*) AS int) AS [Value]
                FROM dbo.PIC_FILES_T
                WHERE Name IN ('Thumb regen photo', 'Route upload photo')
                   OR Name LIKE 'photo-submission-probe-%'
                """)
            .SingleAsync();
        Assert.Equal(0, legacyTestPhotoCount);
    }

    private static QueenZoneDbContext CreateSqlServerContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<QueenZoneDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        return new QueenZoneDbContext(options);
    }

    // Use parameterless string overloads only. string.Contains/StartsWith/EndsWith
    // with StringComparison cannot be translated by EF Core SQL Server.
    // SQL Express mirror collations are case-insensitive, so marker matching still works.

    private static IQueryable<NewsTableRow> NewsResidueQuery(QueenZoneDbContext dbContext) =>
        dbContext.NewsRows.Where(row =>
            (row.Slug != null
                && (row.Slug.StartsWith("probe-write-")
                    || row.Slug.StartsWith("news-section-live-probe-")
                    || row.Slug.StartsWith("full-lifecycle-live-probe-")))
            || row.EditorEmail == "legacy-write-probe@queenzone.local");

    private static IQueryable<NewsAuditLogEntity> NewsAuditResidueQuery(QueenZoneDbContext dbContext) =>
        dbContext.NewsAuditLogs.Where(audit =>
            audit.ActorEmail == "legacy-write-probe@queenzone.local");

    private static IQueryable<NewsAgentRunRequestEntity> RunRequestResidueQuery(QueenZoneDbContext dbContext) =>
        dbContext.NewsAgentRunRequests.Where(request =>
            request.RequestedBy == "url-ingestion-probe@queenzone.local"
            || request.RequestedBy == "url-ingestion-full-probe@queenzone.local"
            || (request.ArticleUrl != null && request.ArticleUrl.Contains("qz-url-ingestion-probe")));

    private static IQueryable<NewsAgentRunnerHeartbeatEntity> HeartbeatResidueQuery(QueenZoneDbContext dbContext) =>
        dbContext.NewsAgentRunnerHeartbeats.Where(heartbeat =>
            heartbeat.RunnerId.Contains("url-ingestion")
            && heartbeat.RunnerId.Contains("probe"));

    private static IQueryable<NewsCandidateEntity> CandidateResidueQuery(QueenZoneDbContext dbContext) =>
        dbContext.NewsCandidates.Where(candidate =>
            candidate.SourceUrl.Contains("qz-url-ingestion-probe")
            || candidate.CanonicalUrl.Contains("qz-url-ingestion-probe")
            || candidate.SourceUrl.Contains("qz-discovery-promo-probe")
            || candidate.CanonicalUrl.Contains("qz-discovery-promo-probe"));

    private static IQueryable<NewsDiscoverySourceEntity> DiscoverySourceResidueQuery(QueenZoneDbContext dbContext) =>
        dbContext.NewsDiscoverySources.Where(source =>
            source.Key.StartsWith("discovery-promo-probe-"));

    private static IQueryable<MemberAccount> MemberResidueQuery(QueenZoneDbContext dbContext) =>
        dbContext.MemberAccounts.Where(member =>
            member.Email.EndsWith("@example.com")
            || member.Email.EndsWith("@example.test")
            || member.Email.EndsWith("@test.local")
            || member.Email.Contains("pm-probe-")
            || member.Email.Contains("forum-write-probe-")
            || member.Email.Contains("photo-submission-probe-")
            || member.Email.Contains("article-submission-probe-")
            || member.Email.Contains("member-account-probe-")
            // uie2e-{runId}-{fixture}-{n}: RealDataPageTest marker convention (Web.E2E),
            // e.g. CommunitySubmissionWorkflowTests member seeding for #546.
            || member.Email.Contains("uie2e-"));

    private static IQueryable<PrivateMessageEntity> PrivateMessageResidueQuery(QueenZoneDbContext dbContext) =>
        dbContext.PrivateMessages.Where(message =>
            message.Body.Contains("Probe concurrent")
            || message.Body.Contains("Probe reply"));

    private static IQueryable<ModernForumThreadEntity> ForumThreadResidueQuery(QueenZoneDbContext dbContext) =>
        dbContext.ModernForumThreads.Where(thread =>
            thread.Title.Contains("forum-write-probe-"));

    private static IQueryable<PhotoSubmissionEntity> PhotoSubmissionResidueQuery(QueenZoneDbContext dbContext) =>
        dbContext.PhotoSubmissions.Where(submission =>
            submission.Title.Contains("photo-submission-probe-")
            || submission.Title.Contains("uie2e-"));

    private static IQueryable<ArticleSubmissionEntity> ArticleSubmissionResidueQuery(QueenZoneDbContext dbContext) =>
        dbContext.ArticleSubmissions.Where(submission =>
            submission.Title.Contains("article-submission-probe-")
            || submission.Title.Contains("uie2e-"));

    // No existing probe seeds NewsSuggestions; this covers CommunitySubmissionWorkflowTests (#546)
    // only. If CleanupCreatedRowsAsync ever fails to run, this catches it in the nightly scan.
    private static IQueryable<NewsSuggestionEntity> NewsSuggestionResidueQuery(QueenZoneDbContext dbContext) =>
        dbContext.NewsSuggestions.Where(suggestion =>
            suggestion.Url.Contains("uie2e-")
            || (suggestion.Title != null && suggestion.Title.Contains("uie2e-")));

    private static IQueryable<PhotoAdminAuditLogEntity> PhotoAdminAuditResidueQuery(QueenZoneDbContext dbContext) =>
        dbContext.PhotoAdminAuditLogs.Where(audit =>
            audit.ActorEmail == "admin@test.local"
            || audit.ActorEmail == "photo-submission-probe@queenzone.local");

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
