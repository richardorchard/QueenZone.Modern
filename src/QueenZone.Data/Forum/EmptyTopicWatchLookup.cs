namespace QueenZone.Data;

/// <summary>
/// Placeholder until #735 Watch storage lands. Registered so the forum-reply
/// hook is real; fan-out is always empty.
/// </summary>
public sealed class EmptyTopicWatchLookup : ITopicWatchLookup
{
    public Task<IReadOnlyList<Guid>> ListMemberIdsAsync(
        int topicId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Guid>>([]);
}
