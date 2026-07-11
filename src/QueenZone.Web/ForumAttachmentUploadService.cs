using QueenZone.Data;
using QueenZone.Storage;

namespace QueenZone.Web;

/// <summary>
/// Uploads forum form attachments to private UGC storage and persists metadata.
/// Rolls back blobs when metadata save fails.
/// </summary>
public sealed class ForumAttachmentUploadService(
    IBlobUploadService blobUploadService,
    IForumAttachmentRepository attachmentRepository,
    TimeProvider timeProvider)
{
    public async Task UploadAndSaveAsync(
        int legacyPostId,
        Guid memberAccountId,
        IReadOnlyList<IFormFile> files,
        CancellationToken cancellationToken = default)
    {
        if (files.Count == 0)
        {
            return;
        }

        var uploaded = new List<(string Container, string BlobName, NewForumAttachment Meta)>();
        try
        {
            var context = new BlobUploadContext
            {
                MemberAccountId = memberAccountId,
            };

            foreach (var file in files)
            {
                await using var stream = file.OpenReadStream();
                var result = await blobUploadService.UploadAsync(
                    stream,
                    file.FileName,
                    BlobUploadContainers.Forum,
                    context,
                    cancellationToken);

                var mime = string.IsNullOrWhiteSpace(result.ContentType)
                    ? ForumAttachmentValidator.GuessContentType(file.FileName)
                    : result.ContentType;

                var meta = new NewForumAttachment(
                    Path.GetFileName(file.FileName),
                    result.BlobName,
                    result.Container,
                    result.SizeBytes,
                    mime,
                    timeProvider.GetUtcNow());

                uploaded.Add((result.Container, result.BlobName, meta));
            }

            await attachmentRepository.AddAttachmentsAsync(
                legacyPostId,
                uploaded.Select(item => item.Meta),
                cancellationToken);
        }
        catch
        {
            foreach (var item in uploaded)
            {
                try
                {
                    await blobUploadService.DeleteAsync(item.Container, item.BlobName, cancellationToken);
                }
                catch
                {
                    // Best-effort cleanup.
                }
            }

            throw;
        }
    }
}
