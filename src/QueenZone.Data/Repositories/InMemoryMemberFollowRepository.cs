using QueenZone.Data.Entities;

namespace QueenZone.Data;

public sealed class InMemoryMemberFollowRepository : IMemberFollowRepository
{
    private readonly List<MemberFollowEntity> follows = [];
    private readonly Lock gate = new();

    public Task<bool> IsFollowingAsync(
        Guid followerMemberId,
        Guid followedMemberId,
        CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            return Task.FromResult(follows.Any(
                follow => follow.FollowerMemberId == followerMemberId
                    && follow.FollowedMemberId == followedMemberId));
        }
    }

    public Task FollowAsync(
        Guid followerMemberId,
        Guid followedMemberId,
        DateTimeOffset followedAt,
        CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            if (follows.Any(
                    follow => follow.FollowerMemberId == followerMemberId
                        && follow.FollowedMemberId == followedMemberId))
            {
                return Task.CompletedTask;
            }

            follows.Add(new MemberFollowEntity
            {
                Id = Guid.NewGuid(),
                FollowerMemberId = followerMemberId,
                FollowedMemberId = followedMemberId,
                CreatedAt = followedAt,
            });
            return Task.CompletedTask;
        }
    }

    public Task<bool> UnfollowAsync(
        Guid followerMemberId,
        Guid followedMemberId,
        CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            var removed = follows.RemoveAll(
                follow => follow.FollowerMemberId == followerMemberId
                    && follow.FollowedMemberId == followedMemberId);
            return Task.FromResult(removed > 0);
        }
    }
}
