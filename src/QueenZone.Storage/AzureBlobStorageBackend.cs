using System.Diagnostics.CodeAnalysis;
using Azure;
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

    public async Task<BlobContent?> OpenReadAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        var container = blobServiceClient.GetBlobContainerClient(containerName);
        var blob = container.GetBlobClient(blobName);
        try
        {
            var properties = await blob.GetPropertiesAsync(cancellationToken: cancellationToken);
            // OpenReadAsync is seekable so app proxies can honour Range for audio.
            var stream = await blob.OpenReadAsync(cancellationToken: cancellationToken);
            return new BlobContent
            {
                Stream = stream,
                ContentType = properties.Value.ContentType ?? "application/octet-stream",
                ETag = properties.Value.ETag.ToString(),
                ContentLength = properties.Value.ContentLength,
                LastModified = properties.Value.LastModified,
            };
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public Uri GetBlobUri(string containerName, string blobName) =>
        blobServiceClient.GetBlobContainerClient(containerName).GetBlobClient(blobName).Uri;
}
