using System.Collections.Concurrent;

namespace QueenZone.Storage;

/// <summary>
/// In-process blob store for unit tests (no Azure dependency).
/// </summary>
internal sealed class InMemoryBlobStorageBackend : IBlobStorageBackend
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, (byte[] Bytes, string ContentType)>> store =
        new(StringComparer.OrdinalIgnoreCase);

    public Uri BaseUri { get; } = new("https://memory.blob.test");

    public Task UploadAsync(
        string containerName,
        string blobName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        using var ms = new MemoryStream();
        content.CopyTo(ms);
        var container = store.GetOrAdd(
            containerName,
            _ => new ConcurrentDictionary<string, (byte[] Bytes, string ContentType)>(StringComparer.OrdinalIgnoreCase));
        container[blobName] = (ms.ToArray(), contentType);
        return Task.CompletedTask;
    }

    public Task DeleteIfExistsAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        if (store.TryGetValue(containerName, out var container))
        {
            container.TryRemove(blobName, out _);
        }

        return Task.CompletedTask;
    }

    public Uri GetBlobUri(string containerName, string blobName) =>
        new($"{BaseUri.AbsoluteUri.TrimEnd('/')}/{containerName}/{blobName}");

    public bool Exists(string containerName, string blobName) =>
        store.TryGetValue(containerName, out var container) && container.ContainsKey(blobName);

    public (byte[] Bytes, string ContentType)? TryGet(string containerName, string blobName)
    {
        if (store.TryGetValue(containerName, out var container)
            && container.TryGetValue(blobName, out var value))
        {
            return value;
        }

        return null;
    }
}
