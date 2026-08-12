using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QueenZone.Data;

namespace QueenZone.Web.Search;

/// <summary>
/// Standalone-worker entry point for issue #527: periodically calls
/// <see cref="SearchReindexBuilder.ReindexAllAsync"/> against the real database, coordinated via
/// <see cref="ISearchReindexRunLeaseService"/> so overlapping external triggers don't run
/// concurrently, and recorded as an <see cref="ISearchReindexRunRequestRepository"/> row so a run
/// that crashes mid-flight is resumable (stale reclaim) rather than silently lost. Mirrors
/// <c>DiscoverNewsWorker</c>'s <c>--scheduled</c> path in <c>QueenZone.NewsAgent</c>.
/// </summary>
public sealed class SearchReindexScheduledWorker(
    SearchReindexBuilder searchReindexBuilder,
    ISearchReindexRunLeaseService runLeaseService,
    ISearchReindexRunRequestRepository runRequestRepository,
    IOptions<SearchReindexSchedulerOptions> schedulerOptions,
    ILogger<SearchReindexScheduledWorker> logger)
{
    public SearchReindexRunSummary? LastRunSummary { get; private set; }

    public async Task<int> RunAsync(
        SearchReindexCommandOptions options,
        CancellationToken cancellationToken = default)
    {
        await using var runLease = await TryAcquireRunLeaseAsync(options, cancellationToken);
        if (runLease is null)
        {
            return Finish(new SearchReindexRunSummary(SkippedDueToLease: true, SkippedNoClaim: false, ExitCode: 0));
        }

        var runnerId = Environment.MachineName;
        var queueResult = await runRequestRepository.QueueAsync(
            new SearchReindexRunRequestCreate(runnerId), cancellationToken);
        var claimed = await runRequestRepository.ClaimNextAsync(runnerId, cancellationToken);
        if (claimed is null || claimed.Id != queueResult.Request.Id)
        {
            return Finish(new SearchReindexRunSummary(SkippedDueToLease: false, SkippedNoClaim: true, ExitCode: 0));
        }

        try
        {
            await searchReindexBuilder.ReindexAllAsync(cancellationToken);
            await runRequestRepository.CompleteAsync(
                claimed.Id, "Search reindex completed successfully.", cancellationToken);
            return Finish(new SearchReindexRunSummary(SkippedDueToLease: false, SkippedNoClaim: false, ExitCode: 0));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Search reindex run {RequestId} failed.", claimed.Id);
            await runRequestRepository.FailAsync(
                claimed.Id,
                $"Run failed unexpectedly ({exception.GetType().Name}). Check the worker logs.",
                cancellationToken);
            return Finish(new SearchReindexRunSummary(SkippedDueToLease: false, SkippedNoClaim: false, ExitCode: 1));
        }
    }

    private int Finish(SearchReindexRunSummary summary)
    {
        LastRunSummary = summary;
        SearchReindexRunTelemetry.LogRunCompleted(logger, summary);
        return summary.ExitCode;
    }

    private async Task<ISearchReindexRunLease?> TryAcquireRunLeaseAsync(
        SearchReindexCommandOptions options,
        CancellationToken cancellationToken)
    {
        var scheduler = schedulerOptions.Value;
        if (!scheduler.UseRunLease || options.Force)
        {
            return NoOpSearchReindexRunLease.Instance;
        }

        var lease = await runLeaseService.TryAcquireAsync(
            scheduler.LeaseName,
            TimeSpan.FromMinutes(scheduler.LeaseDurationMinutes),
            cancellationToken);
        if (lease is null)
        {
            logger.LogWarning(
                "Skipping search-reindex run because lease {LeaseName} is held by another instance.",
                scheduler.LeaseName);
        }

        return lease;
    }

    private sealed class NoOpSearchReindexRunLease : ISearchReindexRunLease
    {
        public static readonly NoOpSearchReindexRunLease Instance = new();

        public string LeaseName => string.Empty;

        public string HolderId => string.Empty;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
