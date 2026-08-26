namespace QueenZone.Data;

/// <summary>
/// Empty Watch list for unit tests that isolate dispatch without Watch storage.
/// Production uses <see cref="ITopicWatchRepository"/>.
/// </summary>
public sealed class EmptyTopicWatchLookup : ITopicWatchLookup
{
    public Task<IReadOnlyList<Guid>> ListMemberIdsAsync(
        int topicId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Guid>>([]);
}
