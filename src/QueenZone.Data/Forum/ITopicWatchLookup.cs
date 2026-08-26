namespace QueenZone.Data;

/// <summary>
/// Members who Watch a forum topic. Never treat prior posters or the topic
/// starter as implicit watchers.
/// </summary>
public interface ITopicWatchLookup
{
    Task<IReadOnlyList<Guid>> ListMemberIdsAsync(
        int topicId,
        CancellationToken cancellationToken = default);
}
