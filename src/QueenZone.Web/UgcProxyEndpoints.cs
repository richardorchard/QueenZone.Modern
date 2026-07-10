using Microsoft.AspNetCore.Mvc;
using QueenZone.Storage;

namespace QueenZone.Web;

/// <summary>
/// Serves private UGC blobs through the web app (no direct Azure blob URLs in HTML).
/// </summary>
public static class UgcProxyEndpoints
{
    public static void MapUgcProxyEndpoints(this WebApplication app)
    {
        app.MapGet(UgcProxyPaths.RouteTemplate, async (
                string area,
                string blobName,
                [FromQuery] string? size,
                [FromServices] IBlobUploadService blobUploadService,
                CancellationToken cancellationToken) =>
            await ServeAsync(area, blobName, size, blobUploadService, cancellationToken))
            .WithName("UgcProxy")
            .AllowAnonymous();
    }

    internal static async Task<IResult> ServeAsync(
        string area,
        string blobName,
        string? size,
        IBlobUploadService blobUploadService,
        CancellationToken cancellationToken)
    {
        var container = UgcProxyPaths.TryMapAreaToContainer(area);
        if (container is null || string.IsNullOrWhiteSpace(blobName))
        {
            return Results.NotFound();
        }

        // Normalize catch-all path segments.
        var normalized = blobName.Trim().TrimStart('/');
        if (string.IsNullOrWhiteSpace(normalized)
            || normalized.Contains("..", StringComparison.Ordinal)
            || normalized.Contains('\\', StringComparison.Ordinal))
        {
            return Results.NotFound();
        }

        var useThumb = string.Equals(size, "thumb", StringComparison.OrdinalIgnoreCase);
        var resolvedName = useThumb
            ? UgcProxyPaths.ToThumbBlobName(normalized)
            : normalized;

        try
        {
            var content = await blobUploadService.OpenReadAsync(
                container,
                resolvedName,
                cancellationToken);

            // Fall back to full image when thumb is missing.
            if (content is null && useThumb)
            {
                content = await blobUploadService.OpenReadAsync(
                    container,
                    normalized,
                    cancellationToken);
            }

            if (content is null)
            {
                return Results.NotFound();
            }

            return Results.Stream(
                content.Stream,
                content.ContentType,
                enableRangeProcessing: false);
        }
        catch (NotSupportedException)
        {
            return Results.NotFound();
        }
    }
}
