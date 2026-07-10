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

        if (!AllowedContainers.Contains(containerName))
        {
            return Results.BadRequest(new { error = "Container is not allowed for editor uploads." });
        }

        var context = BuildUploadContext(httpContext.User);

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await blobUploadService.UploadAsync(
                stream,
                file.FileName,
                containerName,
                context,
                cancellationToken);

            var url = result.PublicUrl
                ?? $"/{result.Container}/{result.BlobName}";

            return Results.Json(new { url, container = result.Container, blobName = result.BlobName });
        }
        catch (NotSupportedException ex)
        {
            return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (BlobUploadException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static BlobUploadContext BuildUploadContext(ClaimsPrincipal user)
    {
        var email = user.FindFirstValue(ClaimTypes.Email)
            ?? user.FindFirstValue("preferred_username")
            ?? user.Identity?.Name;

        return new BlobUploadContext
        {
            ActorEmail = email,
        };
    }
}
