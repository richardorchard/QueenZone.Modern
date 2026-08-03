using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QueenZone.Data;

namespace QueenZone.Web.Tests;

/// <summary>
/// Opt-in Azure SQL / legacy SQL Server probe for admin URL ingestion queue columns
/// and claim lifecycle. Does not fetch remote pages or call OpenRouter.
/// </summary>
[Collection(LiveDatabaseProbeCollection.Name)]
public sealed class EfNewsAgentUrlIngestionLiveProbeTests
{
    [Fact]
    public async Task Url_ingestion_queue_claim_and_complete_on_real_sql_when_probe_enabled()
    {
        if (!IsProbeEnabled(out var connectionString))
        {
            return;
        }

        var services = new ServiceCollection();
        services.AddQueenZoneLegacyData(connectionString);
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<INewsAgentRunRequestRepository>();
        var dbContext = scope.ServiceProvider.GetRequiredService<QueenZoneDbContext>();

        // Fail fast with a clear message if the migration was not applied.
        var kindColumn = await dbContext.Database
            .SqlQueryRaw<string>(
                """
                SELECT c.name AS [Value]
                FROM sys.columns c
                INNER JOIN sys.tables t ON t.object_id = c.object_id
                WHERE t.name = N'NewsAgentRunRequests' AND c.name = N'Kind'
                """)
            .FirstOrDefaultAsync();
        Assert.False(
            string.IsNullOrWhiteSpace(kindColumn),
            "NewsAgentRunRequests.Kind is missing. Apply migration AddNewsAgentUrlIngestionRequests before live URL ingestion.");

        var probeSuffix = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", System.Globalization.CultureInfo.InvariantCulture);
        var articleUrl = $"https://example.com/qz-url-ingestion-probe-{probeSuffix}";
        var draftUrl = $"https://example.com/qz-url-ingestion-probe-draft-{probeSuffix}";
        var runnerId = $"url-ingestion-schema-probe-{probeSuffix}";
        var editor = "url-ingestion-probe@queenzone.local";
        var requestIds = new List<long>();

        try
        {
            var queued = await repository.QueueAsync(new NewsAgentRunRequestCreate(
                RequestedBy: editor,
                Kind: NewsAgentRunRequestKind.UrlIngestion,
                ArticleUrl: articleUrl,
                GenerateDraft: false));
            requestIds.Add(queued.Request.Id);
            Assert.True(queued.WasCreated, "Expected a new URL ingestion request to be created.");
            Assert.Equal(NewsAgentRunRequestKind.UrlIngestion, queued.Request.Kind);
            Assert.Equal(articleUrl, queued.Request.ArticleUrl);
            Assert.False(queued.Request.GenerateDraft);
            Assert.Equal(NewsAgentRunRequestStatus.Pending, queued.Request.Status);

            // A second URL request must also queue (URL rows do not use the gathering ActiveKey).
            var secondUrl = await repository.QueueAsync(new NewsAgentRunRequestCreate(
                editor,
                NewsAgentRunRequestKind.UrlIngestion,
                draftUrl,
                GenerateDraft: true));
            requestIds.Add(secondUrl.Request.Id);
            Assert.True(secondUrl.WasCreated);
            Assert.True(secondUrl.Request.GenerateDraft);

            var listed = await repository.ListRecentAsync(50);
            Assert.Contains(listed, request =>
                request.Id == queued.Request.Id
                && request.Kind == NewsAgentRunRequestKind.UrlIngestion
                && request.ArticleUrl == articleUrl
                && !request.GenerateDraft);
            Assert.Contains(listed, request =>
                request.Id == secondUrl.Request.Id
                && request.GenerateDraft
                && request.ArticleUrl == draftUrl);

            // Only claim/complete our probe rows. Never finish unrelated requests.
            await DrainProbeRequestAsync(repository, runnerId, queued.Request.Id, probeSuffix);
            await DrainProbeRequestAsync(repository, runnerId, secondUrl.Request.Id, probeSuffix);

            var heartbeat = await repository.GetLatestHeartbeatAsync();
            Assert.NotNull(heartbeat);
            Assert.True(
                heartbeat.LastSeenAtUtc >= DateTime.UtcNow.AddMinutes(-5),
                "Expected a recent runner heartbeat after claim attempts.");
        }
        finally
        {
            if (requestIds.Count > 0)
            {
                await dbContext.NewsAgentRunRequests
                    .Where(request => requestIds.Contains(request.Id))
                    .ExecuteDeleteAsync();
            }

            await dbContext.NewsAgentRunnerHeartbeats
                .Where(heartbeat => heartbeat.RunnerId == runnerId)
                .ExecuteDeleteAsync();

            Assert.False(await dbContext.NewsAgentRunRequests
                .AnyAsync(request => requestIds.Contains(request.Id)));
            Assert.False(await dbContext.NewsAgentRunnerHeartbeats
                .AnyAsync(heartbeat => heartbeat.RunnerId == runnerId));
        }
    }

    private static async Task DrainProbeRequestAsync(
        INewsAgentRunRequestRepository repository,
        string runnerId,
        long expectedRequestId,
        string probeSuffix)
    {
        // ClaimNext is FIFO. Skip over foreign pending work by returning non-probe claims.
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var claimed = await repository.ClaimNextAsync(runnerId);
            if (claimed is null)
            {
                Assert.Fail($"Probe request #{expectedRequestId} was never claimed (queue empty).");
            }

            var isOurProbe = claimed.Id == expectedRequestId
                || (claimed.Kind == NewsAgentRunRequestKind.UrlIngestion
                    && claimed.ArticleUrl is not null
                    && claimed.ArticleUrl.Contains(probeSuffix, StringComparison.Ordinal));

            if (!isOurProbe)
            {
                Assert.True(
                    await repository.ReturnToPendingAsync(claimed.Id),
                    $"Could not return foreign request #{claimed.Id} to pending; aborting to avoid finishing production work.");
                // Foreign work is ahead of us; do not loop forever claiming it.
                Assert.Fail(
                    $"Pending non-probe request #{claimed.Id} ({claimed.Kind}) is ahead in the queue. " +
                    "Let the production runner finish it, or complete/cancel it, then re-run this probe.");
            }

            Assert.True(await repository.CompleteAsync(
                claimed.Id,
                "Schema/lifecycle probe completed without outbound fetch."));

            if (claimed.Id == expectedRequestId)
            {
                return;
            }
        }

        Assert.Fail($"Exceeded claim attempts while waiting for probe request #{expectedRequestId}.");
    }

    private static bool IsProbeEnabled(out string connectionString)
    {
        connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__QueenZoneLegacy") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return false;
        }

        return string.Equals(
            Environment.GetEnvironmentVariable("RUN_NEWS_AGENT_URL_INGESTION_PROBE"),
            "true",
            StringComparison.OrdinalIgnoreCase);
    }
}
