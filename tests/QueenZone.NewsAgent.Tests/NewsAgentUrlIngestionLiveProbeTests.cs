using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using QueenZone.Data;
using QueenZone.NewsAgent;

namespace QueenZone.NewsAgent.Tests;

/// <summary>
/// Opt-in full pipeline probe: real SQL + outbound HTTP + triage (OpenRouter when configured).
/// Creates discovery candidates; never publishes to public news.
/// Enable with RUN_NEWS_AGENT_URL_INGESTION_PROBE=true and RUN_NEWS_AGENT_URL_INGESTION_FULL_PROBE=true.
/// </summary>
public sealed class NewsAgentUrlIngestionLiveProbeTests
{
    [Fact]
    public async Task Full_url_ingestion_fetch_and_triage_when_full_probe_enabled()
    {
        if (!IsFullProbeEnabled(out var connectionString))
        {
            return;
        }

        var probeSuffix = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", System.Globalization.CultureInfo.InvariantCulture);
        var articleUrl = Environment.GetEnvironmentVariable("NEWS_AGENT_URL_INGESTION_PROBE_URL");
        if (string.IsNullOrWhiteSpace(articleUrl))
        {
            // Unique query keeps canonical URL distinct; example.com is a stable public HTML target.
            articleUrl = $"https://example.com/?qz-url-ingestion-probe={probeSuffix}";
        }

        var generateDraft = string.Equals(
            Environment.GetEnvironmentVariable("NEWS_AGENT_URL_INGESTION_PROBE_GENERATE_DRAFT"),
            "true",
            StringComparison.OrdinalIgnoreCase);

        if (!OutboundUrlSafety.TryValidatePublicHttpUrl(articleUrl, out var urlError, out var normalizedUrl)
            || string.IsNullOrWhiteSpace(normalizedUrl))
        {
            Assert.Fail($"Probe article URL is not safe for outbound fetch: {urlError}");
        }

        await using var provider = BuildProvider(connectionString);
        await using var scope = provider.CreateAsyncScope();
        var runRequests = scope.ServiceProvider.GetRequiredService<INewsAgentRunRequestRepository>();
        var discovery = scope.ServiceProvider.GetRequiredService<INewsDiscoveryRepository>();
        var processor = scope.ServiceProvider.GetRequiredService<NewsAgentQueuedRunProcessor>();

        var runnerId = Environment.GetEnvironmentVariable("NEWS_AGENT_URL_INGESTION_PROBE_RUNNER_ID");
        if (string.IsNullOrWhiteSpace(runnerId))
        {
            runnerId = $"url-ingestion-full-probe-{probeSuffix}";
        }

        var queued = await runRequests.QueueAsync(new NewsAgentRunRequestCreate(
            "url-ingestion-full-probe@queenzone.local",
            NewsAgentRunRequestKind.UrlIngestion,
            normalizedUrl,
            generateDraft));
        Assert.True(queued.WasCreated, "Expected a new full-probe URL ingestion request.");

        long requestId = queued.Request.Id;
        int? candidateId = null;
        var probeStartedAtUtc = DateTime.UtcNow.AddSeconds(-5);
        try
        {
            var exitCode = await processor.RunOnceAsync(runnerId);
            var recent = await runRequests.ListRecentAsync(25);
            var finished = recent.SingleOrDefault(request => request.Id == requestId);
            Assert.NotNull(finished);

            Assert.True(
                finished.Status is NewsAgentRunRequestStatus.Completed or NewsAgentRunRequestStatus.Failed,
                $"Request {requestId} stuck in {finished.Status}. Summary={finished.Summary}; Error={finished.ErrorMessage}");

            Assert.Equal(0, exitCode);
            Assert.Equal(NewsAgentRunRequestStatus.Completed, finished.Status);
            Assert.False(string.IsNullOrWhiteSpace(finished.Summary));

            var canonicalHash = NewsCandidateDedupe.ComputeUrlHash(normalizedUrl!);
            var candidate = await discovery.GetCandidateByCanonicalUrlHashAsync(canonicalHash);
            // Redirects may change the final canonical URL; fall back to summary candidate id.
            if (candidate is null && finished.Summary is not null)
            {
                var match = System.Text.RegularExpressions.Regex.Match(
                    finished.Summary,
                    @"candidate #(\d+)",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success
                    && int.TryParse(match.Groups[1].Value, out var parsedCandidateId))
                {
                    candidate = await discovery.GetCandidateByIdAsync(parsedCandidateId);
                }
            }

            Assert.NotNull(candidate);
            candidateId = candidate.Id;
            Assert.NotEqual(NewsCandidateStatus.PromotedToArticle, candidate.Status);
            Assert.Null(candidate.PromotedNewsId);

            var evidence = await discovery.GetCandidateEvidenceAsync(candidate.Id);
            Assert.NotEmpty(evidence);

            if (generateDraft)
            {
                // Draft is optional on low-confidence / rejected stories; only assert when NeedsReview/Drafted.
                if (candidate.Status is NewsCandidateStatus.NeedsReview or NewsCandidateStatus.Drafted)
                {
                    var draft = await discovery.GetDraftByCandidateIdAsync(candidate.Id);
                    if (candidate.Status == NewsCandidateStatus.Drafted)
                    {
                        Assert.NotNull(draft);
                    }
                }
            }
            else
            {
                Assert.DoesNotContain("draft generated", finished.Summary, StringComparison.OrdinalIgnoreCase);
            }
        }
        catch
        {
            var recent = await runRequests.ListRecentAsync(25);
            var stuck = recent.SingleOrDefault(request => request.Id == requestId);
            if (stuck?.Status == NewsAgentRunRequestStatus.Running)
            {
                await runRequests.FailAsync(requestId, "Full URL ingestion probe aborted; request marked failed for cleanup.");
            }

            throw;
        }
        finally
        {
            await CleanupProbeArtifactsAsync(
                provider,
                requestId,
                runnerId,
                candidateId,
                probeSuffix,
                normalizedUrl,
                probeStartedAtUtc);
        }
    }

    private static async Task CleanupProbeArtifactsAsync(
        ServiceProvider provider,
        long requestId,
        string runnerId,
        int? candidateId,
        string probeSuffix,
        string normalizedUrl,
        DateTime probeStartedAtUtc)
    {
        await using var cleanupScope = provider.CreateAsyncScope();
        var dbContext = cleanupScope.ServiceProvider.GetRequiredService<QueenZoneDbContext>();

        var candidateIds = await dbContext.NewsCandidates
            .Where(candidate =>
                candidate.PromotedNewsId == null
                && candidate.CreatedAt >= probeStartedAtUtc
                && ((candidateId.HasValue && candidate.Id == candidateId.Value)
                    || candidate.SourceUrl.Contains(probeSuffix)
                    || candidate.CanonicalUrl.Contains(probeSuffix)
                    || candidate.SourceUrl == normalizedUrl
                    || candidate.CanonicalUrl == normalizedUrl))
            .Select(candidate => candidate.Id)
            .ToListAsync();

        if (candidateIds.Count > 0)
        {
            await dbContext.NewsAgentDrafts
                .Where(draft => candidateIds.Contains(draft.CandidateId))
                .ExecuteDeleteAsync();
            await dbContext.NewsCandidateEvidence
                .Where(evidence => candidateIds.Contains(evidence.CandidateId))
                .ExecuteDeleteAsync();
            await dbContext.NewsAiRuns
                .Where(run => candidateIds.Contains(run.CandidateId))
                .ExecuteDeleteAsync();
            await dbContext.NewsCandidates
                .Where(candidate => candidateIds.Contains(candidate.Id))
                .ExecuteDeleteAsync();
        }

        await dbContext.NewsAgentRunRequests
            .Where(request => request.Id == requestId)
            .ExecuteDeleteAsync();
        await dbContext.NewsAgentRunnerHeartbeats
            .Where(heartbeat => heartbeat.RunnerId == runnerId)
            .ExecuteDeleteAsync();

        Assert.False(await dbContext.NewsAgentRunRequests.AnyAsync(request => request.Id == requestId));
        Assert.False(await dbContext.NewsAgentRunnerHeartbeats.AnyAsync(heartbeat => heartbeat.RunnerId == runnerId));
        Assert.False(await dbContext.NewsCandidates.AnyAsync(candidate => candidateIds.Contains(candidate.Id)));
    }

    private static ServiceProvider BuildProvider(string connectionString)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:QueenZoneLegacy"] = connectionString,
                // Prefer OPENROUTER_API_KEY / worker local settings via environment.
                ["OpenRouter:ApiKey"] = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY")
                    ?? Environment.GetEnvironmentVariable("OpenRouter__ApiKey")
                    ?? string.Empty,
                ["OpenRouter:DryRun"] = "false",
                ["NewsAgentScheduler:UseRunLease"] = "false"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddQueenZoneNewsAgentWorker(configuration);
        services.AddQueenZoneLegacyData(connectionString);
        return services.BuildServiceProvider();
    }

    private static bool IsFullProbeEnabled(out string connectionString)
    {
        connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__QueenZoneLegacy") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return false;
        }

        if (!string.Equals(
                Environment.GetEnvironmentVariable("RUN_NEWS_AGENT_URL_INGESTION_PROBE"),
                "true",
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return string.Equals(
            Environment.GetEnvironmentVariable("RUN_NEWS_AGENT_URL_INGESTION_FULL_PROBE"),
            "true",
            StringComparison.OrdinalIgnoreCase);
    }
}
