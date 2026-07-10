namespace QueenZone.Storage;

internal sealed class BlobUploadValidator(BlobUploadOptions options)
{
    public void EnsureKnownContainer(string containerName)
    {
        if (string.IsNullOrWhiteSpace(containerName))
        {
            throw new BlobUploadException("Container name is required.");
        }

        if (!BlobUploadContainers.All.Contains(containerName, StringComparer.OrdinalIgnoreCase)
            && !options.Containers.ContainsKey(containerName))
        {
            throw new BlobUploadException(
                $"Container '{containerName}' is not a known UGC container. " +
                $"Use one of: {string.Join(", ", BlobUploadContainers.All)}.");
        }
    }

    public long GetMaxBytes(string containerName)
    {
        if (options.Containers.TryGetValue(containerName, out var policy)
            && policy.MaxBytes is > 0)
        {
            return policy.MaxBytes.Value;
        }

        return options.DefaultMaxBytes;
    }

    public IReadOnlyList<string> GetAllowedContentTypes(string containerName)
    {
        if (options.Containers.TryGetValue(containerName, out var policy)
            && policy.AllowedContentTypes is { Count: > 0 })
        {
            return policy.AllowedContentTypes;
        }

        return options.DefaultAllowedContentTypes;
    }

    public void ValidateSize(long sizeBytes, string containerName)
    {
        var max = GetMaxBytes(containerName);
        if (sizeBytes <= 0)
        {
            throw new BlobUploadException("Upload content is empty.");
        }

        if (sizeBytes > max)
        {
            throw new BlobUploadException(
                $"Upload is {sizeBytes} bytes, which exceeds the {max}-byte limit for container '{containerName}'.");
        }
    }

    public string ResolveAndValidateContentType(
        string originalFileName,
        ReadOnlySpan<byte> header,
        string containerName)
    {
        var extension = Path.GetExtension(originalFileName);
        var fromSniff = BlobContentSniffer.TryDetectContentType(header);
        var fromExtension = BlobContentSniffer.GuessContentTypeFromExtension(extension);

        // Prefer sniff when available; require agreement with extension when both present.
        string? contentType = fromSniff ?? fromExtension;
        if (fromSniff is not null
            && fromExtension is not null
            && !string.Equals(fromSniff, fromExtension, StringComparison.OrdinalIgnoreCase))
        {
            throw new BlobUploadException(
                $"File content type '{fromSniff}' does not match extension '{extension}' ({fromExtension}).");
        }

        if (contentType is null)
        {
            throw new BlobUploadException(
                $"Unable to determine content type for '{originalFileName}'.");
        }

        var allowed = GetAllowedContentTypes(containerName);
        if (!allowed.Any(item => string.Equals(item, contentType, StringComparison.OrdinalIgnoreCase)))
        {
            throw new BlobUploadException(
                $"Content type '{contentType}' is not allowed for container '{containerName}'. " +
                $"Allowed: {string.Join(", ", allowed)}.");
        }

        return contentType;
    }
}
