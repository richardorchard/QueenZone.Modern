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

    /// <summary>
    /// Opens a readable stream for an existing blob, or returns null when missing.
    /// </summary>
    Task<BlobContent?> OpenReadAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default);

    Uri GetBlobUri(string containerName, string blobName);
}
