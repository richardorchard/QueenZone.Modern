using Azure.Storage.Blobs;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using QueenZone.Storage;

namespace QueenZone.Web.Health;

/// <summary>
/// Readiness: blob storage is reachable when configured.
/// Local NullBlobUploadService / missing BlobServiceClient is healthy "not configured".
/// </summary>
public sealed class BlobReadyHealthCheck(
    IBlobUploadService blobUploadService,
    IServiceProvider serviceProvider,
    ILogger<BlobReadyHealthCheck>? logger = null) : IHealthCheck
{
    private readonly ILogger<BlobReadyHealthCheck> effectiveLogger = logger ?? NullLogger<BlobReadyHealthCheck>.Instance;

    public const string Name = "blob";

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (blobUploadService is NullBlobUploadService)
        {
            return HealthCheckResult.Healthy("Blob storage not configured.");
        }

        var client = serviceProvider.GetService<BlobServiceClient>();
        if (client is null)
        {
            return HealthCheckResult.Healthy("Blob storage not configured.");
        }

        try
        {
            // Lightweight account property fetch — no container or secret material in the response.
            _ = await client.GetPropertiesAsync(cancellationToken);
            return HealthCheckResult.Healthy("Blob storage reachable.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            effectiveLogger.LogWarning(ex, "Blob storage readiness check failed.");
            return HealthCheckResult.Unhealthy("Blob storage check failed.");
        }
    }
}
