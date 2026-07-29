namespace QueenZone.Data;

public interface IForumPollRepository
{
    /// <summary>
    /// Creates a poll for an existing modern thread (by public topic id).
    /// Prefer creating polls inside <see cref="IForumWriteRepository.CreateThreadAsync"/> when starting a thread.
    /// </summary>
    Task<Guid> CreatePollAsync(
        int legacyTopicId,
        NewForumPoll poll,
        CancellationToken cancellationToken = default);

    Task<ForumPollResults?> GetPollWithResultsAsync(
        int legacyTopicId,
        Guid? viewerMemberId,
        bool viewerIsAdmin = false,
        CancellationToken cancellationToken = default);

    Task CastVoteAsync(
        Guid pollId,
        Guid memberId,
        IEnumerable<Guid> optionIds,
        CancellationToken cancellationToken = default);

    Task ClosePollAsync(
        Guid pollId,
        Guid memberId,
        bool isAdmin,
        CancellationToken cancellationToken = default);
}
