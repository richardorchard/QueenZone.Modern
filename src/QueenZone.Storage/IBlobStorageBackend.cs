namespace QueenZone.Storage;

/// <summary>
/// Low-level blob transport. Production uses Azure; tests use an in-memory backend.
/// </summary>
internal interface IBlobStorageBackend
{
    Task UploadAsync(
        string containerName,
        string blobName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);

    Task DeleteIfExistsAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default);

    Uri GetBlobUri(string containerName, string blobName);
}
