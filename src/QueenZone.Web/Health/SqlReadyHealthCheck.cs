using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using QueenZone.Data;

namespace QueenZone.Web.Health;

/// <summary>
/// Readiness: SQL is reachable when a legacy connection string is configured.
/// In-memory / sample-data mode (no DbContext) is treated as healthy "not configured".
/// </summary>
public sealed class SqlReadyHealthCheck : IHealthCheck
{
    public const string Name = "sql";

    /// <summary>
    /// Upper bound for <c>CanConnectAsync</c>. Independent of
    /// <c>QueenZoneDbContext</c>'s <c>EnableRetryOnFailure</c> policy
    /// (5 retries, 20s max delay), which can otherwise exceed 100s on a
    /// cold first connection.
    /// </summary>
    internal static readonly TimeSpan DefaultConnectTimeout = TimeSpan.FromSeconds(15);

    private readonly IServiceScopeFactory scopeFactory;
    private readonly TimeSpan connectTimeout;
    private readonly Func<QueenZoneDbContext, CancellationToken, Task<bool>> canConnectAsync;
    private readonly ILogger<SqlReadyHealthCheck> logger;

    public SqlReadyHealthCheck(IServiceScopeFactory scopeFactory, ILogger<SqlReadyHealthCheck>? logger = null)
        : this(scopeFactory, DefaultConnectTimeout, logger: logger)
    {
    }

    internal SqlReadyHealthCheck(
        IServiceScopeFactory scopeFactory,
        TimeSpan connectTimeout,
        Func<QueenZoneDbContext, CancellationToken, Task<bool>>? canConnectAsync = null,
        ILogger<SqlReadyHealthCheck>? logger = null)
    {
        this.scopeFactory = scopeFactory;
        this.connectTimeout = connectTimeout;
        this.canConnectAsync = canConnectAsync
            ?? ((dbContext, token) => dbContext.Database.CanConnectAsync(token));
        this.logger = logger ?? NullLogger<SqlReadyHealthCheck>.Instance;
    }

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

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(connectTimeout);

        try
        {
            var canConnect = await canConnectAsync(dbContext, timeout.Token);
            return canConnect
                ? HealthCheckResult.Healthy("SQL reachable.")
                : HealthCheckResult.Unhealthy("SQL cannot connect.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy("SQL check timed out.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "SQL readiness check failed.");
            // Do not return exception text — connection strings must not leak to probes.
            return HealthCheckResult.Unhealthy("SQL check failed.");
        }
    }
}
