namespace QueenZone.Storage;

/// <summary>
/// Used when <c>ConnectionStrings:BlobStorage</c> is not configured so the app boots locally.
/// Upload/delete throw <see cref="NotSupportedException"/>.
/// </summary>
public sealed class NullBlobUploadService : IBlobUploadService
{
    public const string NotConfiguredMessage =
        "Blob storage is not configured. Set ConnectionStrings:BlobStorage to enable uploads.";

    public Task<BlobUploadResult> UploadAsync(
        Stream content,
        string originalFileName,
        string containerName,
        BlobUploadContext? context = null,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException(NotConfiguredMessage);

    public Task DeleteAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException(NotConfiguredMessage);
}
