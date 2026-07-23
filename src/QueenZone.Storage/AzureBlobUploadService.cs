using Microsoft.Extensions.Options;

namespace QueenZone.Storage;

public sealed class AzureBlobUploadService : IBlobUploadService
{
    /// <summary>
    /// Prefer buffering small uploads in memory; larger or non-seekable streams go to a temp file
    /// so concurrent multi-MB uploads do not pin full copies on the LOH.
    /// </summary>
    internal const long InMemoryBufferThresholdBytes = 256 * 1024;

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
        var maxBytes = validator.GetMaxBytes(containerName);

        await using var buffer = await BufferForUploadAsync(content, maxBytes, cancellationToken);
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

    public async Task<BlobContent?> OpenReadAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(containerName) || string.IsNullOrWhiteSpace(blobName))
        {
            return null;
        }

        return await backend.OpenReadAsync(containerName, blobName, cancellationToken);
    }

    /// <summary>
    /// Materializes upload content for size validation and content sniffing without always
    /// holding the full payload in a <see cref="MemoryStream"/>.
    /// </summary>
    internal static async Task<Stream> BufferForUploadAsync(
        Stream content,
        long maxBytes,
        CancellationToken cancellationToken)
    {
        if (content.CanSeek)
        {
            var remaining = content.Length - content.Position;
            if (remaining < 0)
            {
                throw new BlobUploadException("Upload content is empty.");
            }

            if (remaining > maxBytes)
            {
                throw new BlobUploadException(
                    $"Upload is {remaining} bytes, which exceeds the {maxBytes}-byte limit.");
            }

            if (remaining <= InMemoryBufferThresholdBytes)
            {
                var memory = new MemoryStream(capacity: (int)remaining);
                await content.CopyToAsync(memory, cancellationToken);
                memory.Position = 0;
                return memory;
            }
        }

        // Non-seekable streams, or large seekable uploads: stream to a temp file with a hard cap.
        var tempPath = Path.Combine(Path.GetTempPath(), "qz-upload-" + Guid.NewGuid().ToString("N"));
        try
        {
            await using (var file = new FileStream(
                             tempPath,
                             FileMode.CreateNew,
                             FileAccess.Write,
                             FileShare.None,
                             bufferSize: 80 * 1024,
                             FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                var buffer = new byte[80 * 1024];
                long total = 0;
                int read;
                while ((read = await content.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
                {
                    total += read;
                    if (total > maxBytes)
                    {
                        throw new BlobUploadException(
                            $"Upload exceeds the {maxBytes}-byte limit.");
                    }

                    await file.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                }
            }

            return new FileStream(
                tempPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 80 * 1024,
                FileOptions.Asynchronous | FileOptions.SequentialScan | FileOptions.DeleteOnClose);
        }
        catch
        {
            TryDeleteTemp(tempPath);
            throw;
        }
    }

    private static void TryDeleteTemp(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Best-effort cleanup; DeleteOnClose handles the happy path.
        }
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
