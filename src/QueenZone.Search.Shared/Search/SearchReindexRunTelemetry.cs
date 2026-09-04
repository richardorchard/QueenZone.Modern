using Microsoft.Extensions.Logging;

namespace QueenZone.Search.Shared;

public sealed record SearchReindexRunSummary(
    bool SkippedDueToLease,
    bool SkippedNoClaim,
    int ExitCode);

public static class SearchReindexRunTelemetry
{
    public static readonly EventId RunCompleted = new(4200, "SearchReindexRunCompleted");

    public static void LogRunCompleted(ILogger logger, SearchReindexRunSummary summary)
    {
        if (summary.SkippedDueToLease)
        {
            logger.LogInformation(
                RunCompleted,
                "Search reindex run skipped because another instance holds the run lease.");
            return;
        }

        if (summary.SkippedNoClaim)
        {
            logger.LogInformation(
                RunCompleted,
                "Search reindex run skipped: queued request was not claimable (already claimed or not yet stale).");
            return;
        }

        logger.LogInformation(
            RunCompleted,
            "Search reindex run completed. ExitCode={ExitCode}",
            summary.ExitCode);
    }
}
