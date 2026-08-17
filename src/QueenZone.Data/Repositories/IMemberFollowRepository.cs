namespace QueenZone.Data;

public interface IMemberFollowRepository
{
    /// <summary>
    /// True when <paramref name="followerMemberId"/> follows <paramref name="followedMemberId"/>.
    /// </summary>
    Task<bool> IsFollowingAsync(
        Guid followerMemberId,
        Guid followedMemberId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a follow. Idempotent when the follow already exists.
    /// Caller must validate self-follow and membership.
    /// </summary>
    Task FollowAsync(
        Guid followerMemberId,
        Guid followedMemberId,
        DateTimeOffset followedAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a follow. Returns false when no such follow existed.
    /// </summary>
    Task<bool> UnfollowAsync(
        Guid followerMemberId,
        Guid followedMemberId,
        CancellationToken cancellationToken = default);
}
