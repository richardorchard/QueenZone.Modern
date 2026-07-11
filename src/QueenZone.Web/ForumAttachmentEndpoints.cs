using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;
using QueenZone.Storage;

namespace QueenZone.Web;

/// <summary>
/// Member-gated forum attachment downloads.
/// Modern attachments stream from private UGC storage; legacy import files redirect
/// through the pictures.queenzone.org Worker (Content-Disposition capable).
/// </summary>
public static class ForumAttachmentEndpoints
{
    public static void MapForumAttachmentEndpoints(this WebApplication app)
    {
        app.MapGet("/forum/attachment/legacy/{legacyPostId:int}", async (
                int legacyPostId,
                IForumAttachmentRepository attachmentRepository,
                CancellationToken cancellationToken) =>
            await ServeLegacyAsync(legacyPostId, attachmentRepository, cancellationToken))
            .RequireAuthorization(MemberAuthenticationSchemes.MemberPolicy)
            .WithName("DownloadLegacyForumAttachment");

        app.MapGet("/forum/attachment/{legacyPostId:int}/{attachmentId:guid}", async (
                int legacyPostId,
                Guid attachmentId,
                IForumAttachmentRepository attachmentRepository,
                IBlobUploadService blobUploadService,
                CancellationToken cancellationToken) =>
            await ServeModernAsync(
                legacyPostId,
                attachmentId,
                attachmentRepository,
                blobUploadService,
                cancellationToken))
            .RequireAuthorization(MemberAuthenticationSchemes.MemberPolicy)
            .WithName("DownloadForumAttachment");
    }

    internal static async Task<IResult> ServeLegacyAsync(
        int legacyPostId,
        IForumAttachmentRepository attachmentRepository,
        CancellationToken cancellationToken)
    {
        var legacy = await attachmentRepository.GetLegacyAsync(legacyPostId, cancellationToken);
        if (legacy is null || string.IsNullOrWhiteSpace(legacy.FileName))
        {
            return Results.NotFound();
        }

        // Sanitize path segments — legacy filenames are bare blob names, not paths.
        var fileName = Path.GetFileName(legacy.FileName.Trim());
        if (string.IsNullOrWhiteSpace(fileName)
            || fileName.Contains("..", StringComparison.Ordinal)
            || fileName.Contains('/', StringComparison.Ordinal)
            || fileName.Contains('\\', StringComparison.Ordinal))
        {
            return Results.NotFound();
        }

        return Results.Redirect(ForumAttachmentPaths.BuildLegacyCdnUrl(fileName));
    }

    internal static async Task<IResult> ServeModernAsync(
        int legacyPostId,
        Guid attachmentId,
        IForumAttachmentRepository attachmentRepository,
        IBlobUploadService blobUploadService,
        CancellationToken cancellationToken)
    {
        var attachment = await attachmentRepository.GetAsync(legacyPostId, attachmentId, cancellationToken);
        if (attachment is null)
        {
            return Results.NotFound();
        }

        try
        {
            var content = await blobUploadService.OpenReadAsync(
                attachment.ContainerName,
                attachment.BlobPath,
                cancellationToken);

            if (content is null)
            {
                return Results.NotFound();
            }

            await attachmentRepository.IncrementDownloadCountAsync(attachmentId, cancellationToken);

            var contentType = string.IsNullOrWhiteSpace(attachment.MimeType)
                ? content.ContentType ?? MediaTypeNames.Application.Octet
                : attachment.MimeType;

            // Prefer original filename for Content-Disposition download.
            return Results.File(
                content.Stream,
                contentType,
                fileDownloadName: attachment.OriginalFileName,
                enableRangeProcessing: false);
        }
        catch (NotSupportedException)
        {
            return Results.NotFound();
        }
    }
}
