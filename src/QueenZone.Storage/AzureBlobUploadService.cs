using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

namespace QueenZone.Storage;

public sealed class AzureBlobUploadService : IBlobUploadService
{
    private readonly BlobServiceClient blobServiceClient;
    private readonly BlobUploadOptions options;
    private readonly BlobUploadValidator validator;

    public AzureBlobUploadService(BlobServiceClient blobServiceClient, IOptions<BlobUploadOptions> options)
    {
        this.blobServiceClient = blobServiceClient;
        this.options = options.Value;
        validator = new BlobUploadValidator(this.options);
    }

    public async Task<BlobUploadResult> UploadAsync(
        Stream content,
        string originalFileName,
        string containerName,
        BlobUploadContext? context = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            throw new BlobUploadException("Original file name is required.");
        }

        validator.EnsureKnownContainer(containerName);

        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        var sizeBytes = buffer.Length;
        validator.ValidateSize(sizeBytes, containerName);

        buffer.Position = 0;
        var headerLength = (int)Math.Min(64, buffer.Length);
        var header = new byte[headerLength];
        var read = await buffer.ReadAsync(header.AsMemory(0, headerLength), cancellationToken);
        buffer.Position = 0;

        var contentType = validator.ResolveAndValidateContentType(
            originalFileName,
            header.AsSpan(0, read),
            containerName);

        var blobName = BlobNameGenerator.Create(originalFileName, context);
        var container = blobServiceClient.GetBlobContainerClient(containerName);
        await container.CreateIfNotExistsAsync(
            PublicAccessType.None,
            cancellationToken: cancellationToken);

        var blob = container.GetBlobClient(blobName);
        await blob.UploadAsync(
            buffer,
            new BlobHttpHeaders { ContentType = contentType },
            cancellationToken: cancellationToken);

        return new BlobUploadResult
        {
            Container = containerName,
            BlobName = blobName,
            ContentType = contentType,
            SizeBytes = sizeBytes,
            PublicUrl = BuildPublicUrl(containerName, blobName, blob.Uri),
        };
    }

    public async Task DeleteAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(containerName))
        {
            throw new BlobUploadException("Container name is required.");
        }

        if (string.IsNullOrWhiteSpace(blobName))
        {
            throw new BlobUploadException("Blob name is required.");
        }

        var container = blobServiceClient.GetBlobContainerClient(containerName);
        var blob = container.GetBlobClient(blobName);
        await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    private string BuildPublicUrl(string containerName, string blobName, Uri storageUri)
    {
        if (!string.IsNullOrWhiteSpace(options.PublicBaseUrl))
        {
            return $"{options.PublicBaseUrl.TrimEnd('/')}/{containerName.Trim('/')}/{blobName.TrimStart('/')}";
        }

        return storageUri.ToString();
    }
}
