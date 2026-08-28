namespace QueenZone.Data;

/// <summary>
/// Batched discussion fields for news list/detail. Does not load list bodies.
/// </summary>
public interface INewsForumDiscussionLookup
{
    Task<IReadOnlyDictionary<int, int>> GetReplyCountsAsync(
        IReadOnlyList<int> topicIds,
        CancellationToken cancellationToken = default);

    Task<(int ReplyCount, IReadOnlyList<NewsDiscussionPreview> Preview)> GetDiscussionAsync(
        int topicId,
        int previewCount,
        CancellationToken cancellationToken = default);
}
