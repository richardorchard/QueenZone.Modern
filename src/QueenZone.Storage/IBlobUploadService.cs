namespace QueenZone.Storage;

public interface IBlobUploadService
{
    Task<BlobUploadResult> UploadAsync(
        Stream content,
        string originalFileName,
        string containerName,
        BlobUploadContext? context = null,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens a readable stream for an existing blob, or returns null when missing.
    /// </summary>
    Task<BlobContent?> OpenReadAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default);
}
