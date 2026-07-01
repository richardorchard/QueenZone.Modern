using Microsoft.Extensions.Logging.Abstractions;
using QueenZone.NewsAgent;

namespace QueenZone.Web.Tests;

public sealed class NewsAgentRunTelemetryTests
{
    [Fact]
    public void LogRunCompleted_emits_structured_summary_without_throwing()
    {
        var logger = NullLogger<DiscoverNewsWorker>.Instance;
        var summary = new NewsAgentRunSummary(
            SkippedDueToLease: false,
            AiEnabled: true,
            DryRun: false,
            Discovery: new NewsDiscoveryRunResult(3, 1, 5, 2, 1, 0, 0, []),
            Triage: new NewsTriageRunResult(2, 1, 1, 0, 0, 0, []),
            Draft: new NewsDraftRunResult(1, 1, 0, 0, []),
            EstimatedAiSpendUsd: 0.12m,
            ExitCode: 0);

        var exception = Record.Exception(() => NewsAgentRunTelemetry.LogRunCompleted(logger, summary));

        Assert.Null(exception);
        Assert.Equal(0, summary.TotalFailures);
    }
}
