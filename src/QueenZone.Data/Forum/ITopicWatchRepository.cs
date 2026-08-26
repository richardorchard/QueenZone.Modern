namespace QueenZone.Data;

/// <summary>
/// Per-topic Watch storage. Dispatch (#759) uses <see cref="ITopicWatchLookup"/>;
/// this interface is the write/read surface for Watch / Unwatch.
/// </summary>
public interface ITopicWatchRepository : ITopicWatchLookup
{
    Task<bool> IsWatchingAsync(
        Guid memberAccountId,
        int topicId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a Watch. Idempotent when the pair already exists.
    /// </summary>
    Task WatchAsync(
        Guid memberAccountId,
        int topicId,
        DateTimeOffset watchedAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a Watch. Returns false when no such Watch existed.
    /// </summary>
    Task<bool> UnwatchAsync(
        Guid memberAccountId,
        int topicId,
        CancellationToken cancellationToken = default);
}
