namespace QueenZone.Storage;

/// <summary>
/// Readable blob payload. Dispose to release the underlying stream.
/// </summary>
public sealed class BlobContent : IAsyncDisposable
{
    public required Stream Stream { get; init; }

    public required string ContentType { get; init; }

    public async ValueTask DisposeAsync()
    {
        await Stream.DisposeAsync();
    }
}
