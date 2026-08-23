using Microsoft.Extensions.Caching.Memory;
using QueenZone.Data;
using QueenZone.Storage;

namespace QueenZone.Web;

/// <summary>
/// Resolves track duration for the mobile JSON API. Prefers MPEG headers from
/// the private <c>songfiles</c> blob (cached), then the optional domain value
/// used by sample data. Never throws: missing blobs stay <c>null</c>.
/// </summary>
public sealed class FanPerformanceDurationResolver(
    IBlobUploadService blobUploadService,
    IMemoryCache cache)
{
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromHours(24);
    private const int MaxConcurrentBlobReads = 4;

    public async Task<int?> ResolveAsync(FanPerformance performance, CancellationToken cancellationToken)
    {
        var cacheKey =
            $"fan-performance-duration:{performance.Id}:{performance.FileSizeBytes}:{performance.AudioFileName}";
        if (cache.TryGetValue(cacheKey, out CachedDuration cached))
        {
            return cached.Seconds;
        }

        var fromBlob = await TryReadFromBlobAsync(performance, cancellationToken);
        var value = fromBlob ?? performance.DurationSeconds;
        cache.Set(cacheKey, new CachedDuration(value), CacheLifetime);
        return value;
    }

    public async Task<IReadOnlyList<int?>> ResolveManyAsync(
        IReadOnlyList<FanPerformance> items,
        CancellationToken cancellationToken)
    {
        if (items.Count == 0)
        {
            return [];
        }

        var results = new int?[items.Count];
        using var gate = new SemaphoreSlim(MaxConcurrentBlobReads);
        var tasks = new Task[items.Count];
        for (var i = 0; i < items.Count; i++)
        {
            var index = i;
            var item = items[i];
            tasks[i] = ResolveIndexedAsync(gate, results, index, item, cancellationToken);
        }

        await Task.WhenAll(tasks);
        return results;
    }

    private async Task ResolveIndexedAsync(
        SemaphoreSlim gate,
        int?[] results,
        int index,
        FanPerformance item,
        CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            results[index] = await ResolveAsync(item, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<int?> TryReadFromBlobAsync(
        FanPerformance performance,
        CancellationToken cancellationToken)
    {
        if (!SongFileUrl.IsSafeBlobName(performance.AudioFileName))
        {
            return null;
        }

        var blobName = SongFileUrl.GetBlobName(performance.AudioFileName);
        if (string.IsNullOrWhiteSpace(blobName))
        {
            return null;
        }

        try
        {
            await using var content = await blobUploadService.OpenReadAsync(
                SongFileUrl.ContainerName,
                blobName,
                cancellationToken);
            if (content is null)
            {
                return null;
            }

            var prefix = new byte[Mp3Duration.PrefixBytes];
            var read = await ReadPrefixAsync(content.Stream, prefix, cancellationToken);
            if (read <= 0)
            {
                return null;
            }

            var length = performance.FileSizeBytes;
            if (content.Stream.CanSeek && content.Stream.Length > 0)
            {
                length = content.Stream.Length;
            }

            return Mp3Duration.TryGetSeconds(prefix.AsSpan(0, read), length);
        }
        catch (NotSupportedException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    private static async Task<int> ReadPrefixAsync(
        Stream stream,
        byte[] buffer,
        CancellationToken cancellationToken)
    {
        var total = 0;
        while (total < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(total, buffer.Length - total), cancellationToken);
            if (read == 0)
            {
                break;
            }

            total += read;
        }

        return total;
    }

    private readonly record struct CachedDuration(int? Seconds);
}
