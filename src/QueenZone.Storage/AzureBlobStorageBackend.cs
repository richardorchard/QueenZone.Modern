using System.Diagnostics.CodeAnalysis;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace QueenZone.Storage;

/// <summary>
/// Azure SDK transport. Network paths are covered by opt-in integration tests.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class AzureBlobStorageBackend(BlobServiceClient blobServiceClient) : IBlobStorageBackend
{
    public async Task UploadAsync(
        string containerName,
        string blobName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var container = blobServiceClient.GetBlobContainerClient(containerName);
        await container.CreateIfNotExistsAsync(
            PublicAccessType.None,
            cancellationToken: cancellationToken);

        var blob = container.GetBlobClient(blobName);
        await blob.UploadAsync(
            content,
            new BlobHttpHeaders { ContentType = contentType },
            cancellationToken: cancellationToken);
    }

    public async Task DeleteIfExistsAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        var container = blobServiceClient.GetBlobContainerClient(containerName);
        var blob = container.GetBlobClient(blobName);
        await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    public Uri GetBlobUri(string containerName, string blobName) =>
        blobServiceClient.GetBlobContainerClient(containerName).GetBlobClient(blobName).Uri;
}
