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
}
