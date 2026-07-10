using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Antiforgery;
using QueenZone.Storage;

namespace QueenZone.Web;

public static class EditorImageUploadEndpoints
{
    public const string Route = "/api/uploads/editor-image";

    public const long MaxImageBytes = 5 * 1024 * 1024;

    public static readonly IReadOnlySet<string> AllowedContainers =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            BlobUploadContainers.Forum,
            BlobUploadContainers.Articles,
            BlobUploadContainers.Photos,
            BlobUploadContainers.Avatars,
        };

    public static void MapEditorImageUploadEndpoints(this WebApplication app)
    {
        app.MapPost(Route, async (
                HttpContext httpContext,
                IFormFile? file,
                [FromForm] string? container,
                IBlobUploadService blobUploadService,
                IAntiforgery antiforgery,
                CancellationToken cancellationToken) =>
            await UploadAsync(
                httpContext,
                file,
                container,
                blobUploadService,
                antiforgery,
                cancellationToken))
            .RequireAuthorization("Authoring")
            .DisableAntiforgery()
            .WithName("UploadEditorImage")
            .Accepts<IFormFile>("multipart/form-data");
    }

    internal static async Task<IResult> UploadAsync(
        HttpContext httpContext,
        IFormFile? file,
        string? container,
        IBlobUploadService blobUploadService,
        IAntiforgery antiforgery,
        CancellationToken cancellationToken)
    {
        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            return Results.Unauthorized();
        }

        try
        {
            await antiforgery.ValidateRequestAsync(httpContext);
        }
        catch (AntiforgeryValidationException)
        {
            return Results.BadRequest(new { error = "Invalid antiforgery token." });
        }

        if (file is null || file.Length <= 0)
        {
            return Results.BadRequest(new { error = "An image file is required." });
        }

        if (file.Length > MaxImageBytes)
        {
            return Results.BadRequest(new { error = $"Image must be {MaxImageBytes} bytes or smaller." });
        }

        var contentType = file.ContentType ?? string.Empty;
        if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest(new { error = "Only image uploads are allowed." });
        }

        var containerName = string.IsNullOrWhiteSpace(container)
            ? BlobUploadContainers.Forum
            : container.Trim();

        if (!AllowedContainers.Contains(containerName)
            || UgcProxyPaths.TryMapContainerToArea(containerName) is null)
        {
            return Results.BadRequest(new { error = "Container is not allowed for editor uploads." });
        }

        var context = BuildUploadContext(httpContext.User);
        string? fullBlobName = null;
        string? thumbBlobName = null;

        try
        {
            await using var source = file.OpenReadStream();
            var processed = await EditorImageProcessor.ProcessAsync(
                source,
                file.FileName,
                cancellationToken);

            await using (processed.FullImage)
            await using (processed.Thumbnail)
            {
                // Stable WebP names so full + thumb stay paired under the same prefix.
                fullBlobName = BuildWebpBlobName(context);
                thumbBlobName = UgcProxyPaths.ToThumbBlobName(fullBlobName);

                var fullResult = await blobUploadService.UploadAsync(
                    processed.FullImage,
                    fullBlobName,
                    containerName,
                    CloneContext(context, fullBlobName),
                    cancellationToken);

                fullBlobName = fullResult.BlobName;
                thumbBlobName = UgcProxyPaths.ToThumbBlobName(fullBlobName);

                processed.Thumbnail.Position = 0;
                await blobUploadService.UploadAsync(
                    processed.Thumbnail,
                    thumbBlobName,
                    containerName,
                    CloneContext(context, thumbBlobName),
                    cancellationToken);

                // Product contract: app proxy paths only (not Azure blob / CDN raw URLs).
                var url = UgcProxyPaths.GetPath(fullResult.Container, fullBlobName);
                var thumbUrl = UgcProxyPaths.GetPath(fullResult.Container, thumbBlobName);

                return Results.Json(new
                {
                    url,
                    thumbUrl,
                    container = fullResult.Container,
                    blobName = fullBlobName,
                });
            }
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (NotSupportedException ex)
        {
            return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (BlobUploadException ex)
        {
            // Best-effort cleanup if thumb failed after full succeeded.
            await TryDeleteQuietlyAsync(blobUploadService, containerName, fullBlobName, cancellationToken);
            await TryDeleteQuietlyAsync(blobUploadService, containerName, thumbBlobName, cancellationToken);
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static string BuildWebpBlobName(BlobUploadContext context)
    {
        // Mirror BlobNameGenerator layout with a .webp extension.
        var id = Guid.NewGuid().ToString("N");
        if (context.MemberAccountId is Guid accountId && accountId != Guid.Empty)
        {
            return $"members/{accountId:N}/{id}.webp";
        }

        if (!string.IsNullOrWhiteSpace(context.ActorEmail))
        {
            var slug = SanitizeEmailSegment(context.ActorEmail);
            return $"editors/{slug}/{id}.webp";
        }

        return $"anonymous/{id}.webp";
    }

    private static string SanitizeEmailSegment(string value)
    {
        var trimmed = value.Trim().ToLowerInvariant();
        var chars = trimmed
            .Select(ch =>
            {
                if (char.IsAsciiLetterOrDigit(ch) || ch is '-' or '_' or '.')
                {
                    return ch;
                }

                return ch is '@' or '+' ? '-' : '\0';
            })
            .Where(ch => ch != '\0')
            .ToArray();

        var result = new string(chars).Trim('-', '.');
        return string.IsNullOrEmpty(result) ? "unknown" : result;
    }

    private static async Task TryDeleteQuietlyAsync(
        IBlobUploadService blobUploadService,
        string containerName,
        string? blobName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(blobName))
        {
            return;
        }

        try
        {
            await blobUploadService.DeleteAsync(containerName, blobName, cancellationToken);
        }
        catch
        {
            // Cleanup is best-effort.
        }
    }

    private static BlobUploadContext BuildUploadContext(ClaimsPrincipal user)
    {
        var email = user.FindFirstValue(ClaimTypes.Email)
            ?? user.FindFirstValue("preferred_username")
            ?? user.Identity?.Name;

        Guid? memberAccountId = null;
        var memberIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(memberIdValue, out var parsed) && parsed != Guid.Empty)
        {
            memberAccountId = parsed;
        }

        return new BlobUploadContext
        {
            ActorEmail = email,
            MemberAccountId = memberAccountId,
        };
    }

    private static BlobUploadContext CloneContext(BlobUploadContext source, string preferredBlobName) =>
        new()
        {
            MemberAccountId = source.MemberAccountId,
            MemberId = source.MemberId,
            ActorEmail = source.ActorEmail,
            PreferredBlobName = preferredBlobName,
        };
}
