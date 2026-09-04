using System.Diagnostics;

namespace QueenZone.Web;

/// <summary>
/// Periodically purges pending audio for Rejected and Withdrawn fan-performance
/// submissions after the 30-day grace period.
/// </summary>
public sealed class FanPerformanceSubmissionPurgeHostedService(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<FanPerformanceSubmissionPurgeHostedService> logger) : BackgroundService
{
    internal static readonly TimeSpan DefaultStartupDelay = TimeSpan.FromMinutes(5);

    internal static readonly TimeSpan DefaultRunInterval = TimeSpan.FromHours(24);

    internal TimeSpan StartupDelay { get; init; } = DefaultStartupDelay;

    internal TimeSpan RunInterval { get; init; } = DefaultRunInterval;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(StartupDelay, timeProvider, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            using (var activity = QueenZoneTelemetry.ActivitySource.StartActivity(
                "FanPerformanceSubmissionPurge",
                ActivityKind.Internal))
            {
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var service = scope.ServiceProvider.GetRequiredService<FanPerformanceSubmissionPurgeService>();
                    var result = await service.PurgeAsync(stoppingToken);
                    if (result.Deleted > 0 || result.Failures > 0)
                    {
                        logger.LogInformation(
                            "Purged {Deleted} pending fan-performance blob(s) ({Failures} failure(s), {Candidates} candidate(s)).",
                            result.Deleted,
                            result.Failures,
                            result.Candidates);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Fan-performance submission purge failed.");
                }
            }

            await Task.Delay(RunInterval, timeProvider, stoppingToken);
        }
    }
}
