using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using QueenZone.Data;

namespace QueenZone.Web.Health;

/// <summary>
/// Readiness: SQL is reachable when a legacy connection string is configured.
/// In-memory / sample-data mode (no DbContext) is treated as healthy "not configured".
/// </summary>
public sealed class SqlReadyHealthCheck(IServiceScopeFactory scopeFactory) : IHealthCheck
{
    public const string Name = "sql";

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetService<QueenZoneDbContext>();
        if (dbContext is null)
        {
            return HealthCheckResult.Healthy("SQL not configured (in-memory data).");
        }

        try
        {
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("SQL reachable.")
                : HealthCheckResult.Unhealthy("SQL cannot connect.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            // Do not return exception text — connection strings must not leak to probes.
            return HealthCheckResult.Unhealthy("SQL check failed.");
        }
    }
}
