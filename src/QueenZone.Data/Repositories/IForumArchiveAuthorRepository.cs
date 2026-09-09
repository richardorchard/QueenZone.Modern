namespace QueenZone.Data;

/// <summary>
/// Reads all posts by a single unlinked legacy forum author (keyed by their stable legacy user
/// id, not their display name, since the same id can carry different display-name spellings
/// across posts). Once an author links their legacy account, their activity is served by
/// <see cref="IMemberPublicActivityRepository"/> instead.
/// </summary>
public interface IForumArchiveAuthorRepository
{
    Task<ForumArchiveAuthorSummary?> GetSummaryAsync(
        int legacyUserId,
        CancellationToken cancellationToken = default);

    Task<MemberPublicActivityPage> GetPostsPageAsync(
        int legacyUserId,
        int page,
        int pageSize,
        int totalCount,
        CancellationToken cancellationToken = default);
}
