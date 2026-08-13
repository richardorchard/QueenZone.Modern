namespace QueenZone.Web;

public sealed class MemberAccountDeletionHostedService(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<MemberAccountDeletionHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan RunInterval = TimeSpan.FromHours(6);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<MemberAccountService>();
                var purged = await service.PurgeDueDeletionsAsync(
                    timeProvider.GetUtcNow().UtcDateTime,
                    stoppingToken);
                if (purged > 0)
                {
                    logger.LogInformation(
                        "Purged personal data for {PurgedAccountCount} deleted member account(s).",
                        purged);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Member account deletion purge failed.");
            }

            await Task.Delay(RunInterval, timeProvider, stoppingToken);
        }
    }
}
