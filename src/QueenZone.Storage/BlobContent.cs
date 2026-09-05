namespace QueenZone.Storage;

/// <summary>
/// Readable blob payload. Dispose to release the underlying stream.
/// Optional metadata lets authenticated proxies expose an opaque ETag and
/// Content-Length without revealing blob names or URLs.
/// </summary>
public sealed class BlobContent : IAsyncDisposable
{
    public required Stream Stream { get; init; }

    public required string ContentType { get; init; }

    /// <summary>Opaque blob revision. Already quoted when sourced from Azure.</summary>
    public string? ETag { get; init; }

    public long? ContentLength { get; init; }

    public DateTimeOffset? LastModified { get; init; }

    public async ValueTask DisposeAsync()
    {
        await Stream.DisposeAsync();
    }
}
