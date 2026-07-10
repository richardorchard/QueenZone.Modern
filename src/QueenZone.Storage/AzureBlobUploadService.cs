using Microsoft.Extensions.Options;

namespace QueenZone.Storage;

public sealed class AzureBlobUploadService : IBlobUploadService
{
    private readonly IBlobStorageBackend backend;
    private readonly BlobUploadOptions options;
    private readonly BlobUploadValidator validator;

    internal AzureBlobUploadService(IBlobStorageBackend backend, IOptions<BlobUploadOptions> options)
    {
        this.backend = backend;
        this.options = options.Value;
        validator = new BlobUploadValidator(this.options);
    }

    /// <summary>
    /// Production constructor used by DI (Azure backend).
    /// </summary>
    public AzureBlobUploadService(Azure.Storage.Blobs.BlobServiceClient blobServiceClient, IOptions<BlobUploadOptions> options)
        : this(new AzureBlobStorageBackend(blobServiceClient), options)
    {
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
        await backend.UploadAsync(containerName, blobName, buffer, contentType, cancellationToken);

        var storageUri = backend.GetBlobUri(containerName, blobName);
        return new BlobUploadResult
        {
            Container = containerName,
            BlobName = blobName,
            ContentType = contentType,
            SizeBytes = sizeBytes,
            PublicUrl = BuildPublicUrl(containerName, blobName, storageUri),
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

        await backend.DeleteIfExistsAsync(containerName, blobName, cancellationToken);
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
