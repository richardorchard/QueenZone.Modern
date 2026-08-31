using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace QueenZone.Data;

/// <summary>
/// Probes legacy <c>NEWS_T</c> column availability during host startup so the result is already
/// cached by the time the first request constructs a news repository (issue #1161) — avoiding a
/// synchronous database round trip on a request thread at cold start.
/// </summary>
public sealed class NewsColumnAvailabilityWarmupService(
    IDbContextFactory<QueenZoneDbContext> dbContextFactory,
    ILogger<NewsColumnAvailabilityWarmupService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Best-effort: a failed probe here must not block startup. Repositories fall back to
        // probing lazily (and synchronously) on first use if the cache is still empty.
        try
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            var connectionString = dbContext.Database.GetConnectionString()
                ?? throw new InvalidOperationException("QueenZone legacy database connection string is not configured.");
            await LegacyNewsSchema.WarmupAsync(connectionString, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(
                ex,
                "Legacy NEWS_T column-availability probe failed at startup; repositories will fall back to probing lazily.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
