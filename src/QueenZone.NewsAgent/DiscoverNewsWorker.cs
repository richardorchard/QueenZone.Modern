using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QueenZone.Data;

namespace QueenZone.NewsAgent;

public sealed class DiscoverNewsWorker(
    NewsDiscoveryService discoveryService,
    NewsTriageService triageService,
    NewsDraftGenerationService draftGenerationService,
    NewsAiRunExecutor aiRunExecutor,
    INewsAgentRunLeaseService runLeaseService,
    IOptions<OpenRouterOptions> openRouterOptions,
    IOptions<NewsAgentSchedulerOptions> schedulerOptions,
    ILogger<DiscoverNewsWorker> logger)
{
    public async Task<int> RunAsync(
        DiscoverNewsCommandOptions options,
        CancellationToken cancellationToken = default)
    {
        LogAiStatus();

        await using var runLease = await TryAcquireRunLeaseAsync(options, cancellationToken);
        if (runLease is null)
        {
            return 0;
        }

        var exitCode = 0;

        if (!options.TriageOnly && !options.DraftOnly)
        {
            exitCode = await RunDiscoveryAsync(options, cancellationToken);
        }

        if (options.Triage)
        {
            exitCode = Math.Max(exitCode, await RunTriageAsync(options, cancellationToken));
        }

        if (options.Draft)
        {
            exitCode = Math.Max(exitCode, await RunDraftGenerationAsync(options, cancellationToken));
        }

        return exitCode;
    }

    private async Task<INewsAgentRunLease?> TryAcquireRunLeaseAsync(
        DiscoverNewsCommandOptions options,
        CancellationToken cancellationToken)
    {
        var scheduler = schedulerOptions.Value;
        if (!scheduler.UseRunLease || options.Force)
        {
            return NoOpNewsAgentRunLease.Instance;
        }

        var lease = await runLeaseService.TryAcquireAsync(
            scheduler.LeaseName,
            TimeSpan.FromMinutes(scheduler.LeaseDurationMinutes),
            cancellationToken);
        if (lease is null)
        {
            logger.LogWarning(
                "Skipping discover-news run because lease {LeaseName} is held by another instance.",
                scheduler.LeaseName);
        }

        return lease;
    }

    private void LogAiStatus()
    {
        if (!aiRunExecutor.IsAiEnabled)
        {
            logger.LogWarning("OpenRouter AI processing is disabled. Fetch-only discovery will continue without AI triage or drafting.");
            return;
        }

        if (openRouterOptions.Value.DryRun)
        {
            logger.LogInformation("OpenRouter dry-run mode is enabled. AI requests will be logged without calling the provider.");
        }
    }

    private async Task<int> RunDiscoveryAsync(
        DiscoverNewsCommandOptions options,
        CancellationToken cancellationToken)
    {
        var runOptions = new NewsDiscoveryRunOptions(
            SeedSources: options.SeedSources,
            FetchOnly: true,
            DryRun: options.DryRun,
            Force: options.Force);

        var result = await discoveryService.RunFetchAsync(runOptions, cancellationToken);

        logger.LogInformation(
            "Discovery finished. Sources checked={SourcesChecked}, skipped={SourcesSkipped}, items={ItemsFetched}, created={CandidatesCreated}, duplicates={DuplicatesSkipped}, keyword filtered={KeywordFiltered}, failures={Failures}.",
            result.SourcesChecked,
            result.SourcesSkipped,
            result.ItemsFetched,
            result.CandidatesCreated,
            result.DuplicatesSkipped,
            result.KeywordFiltered,
            result.Failures);

        foreach (var error in result.Errors)
        {
            logger.LogError("Discovery error: {Error}", error);
        }

        return result.Failures > 0 ? 1 : 0;
    }

    private async Task<int> RunTriageAsync(
        DiscoverNewsCommandOptions options,
        CancellationToken cancellationToken)
    {
        var triageResult = await triageService.RunTriageAsync(
            new NewsTriageRunOptions(DryRun: options.DryRun),
            cancellationToken);

        logger.LogInformation(
            "Triage finished. Considered={CandidatesConsidered}, promoted={PromotedToReview}, rejected={Rejected}, duplicates={MarkedDuplicate}, skipped={Skipped}, failures={Failures}.",
            triageResult.CandidatesConsidered,
            triageResult.PromotedToReview,
            triageResult.Rejected,
            triageResult.MarkedDuplicate,
            triageResult.Skipped,
            triageResult.Failures);

        foreach (var error in triageResult.Errors)
        {
            logger.LogError("Triage error: {Error}", error);
        }

        return triageResult.Failures > 0 ? 1 : 0;
    }

    private async Task<int> RunDraftGenerationAsync(
        DiscoverNewsCommandOptions options,
        CancellationToken cancellationToken)
    {
        var draftResult = await draftGenerationService.RunDraftGenerationAsync(
            new NewsDraftRunOptions(
                DryRun: options.DryRun,
                ForceRegenerate: options.Force),
            cancellationToken);

        logger.LogInformation(
            "Draft generation finished. Considered={CandidatesConsidered}, created={DraftsCreated}, skipped={Skipped}, failures={Failures}.",
            draftResult.CandidatesConsidered,
            draftResult.DraftsCreated,
            draftResult.Skipped,
            draftResult.Failures);

        foreach (var error in draftResult.Errors)
        {
            logger.LogError("Draft generation error: {Error}", error);
        }

        return draftResult.Failures > 0 ? 1 : 0;
    }

    private sealed class NoOpNewsAgentRunLease : INewsAgentRunLease
    {
        public static readonly NoOpNewsAgentRunLease Instance = new();

        public string LeaseName => string.Empty;

        public string HolderId => string.Empty;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
