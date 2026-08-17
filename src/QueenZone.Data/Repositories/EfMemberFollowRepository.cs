using Microsoft.EntityFrameworkCore;
using QueenZone.Data.Entities;

namespace QueenZone.Data;

public sealed class EfMemberFollowRepository(QueenZoneDbContext dbContext) : IMemberFollowRepository
{
    public Task<bool> IsFollowingAsync(
        Guid followerMemberId,
        Guid followedMemberId,
        CancellationToken cancellationToken = default) =>
        dbContext.MemberFollows
            .AsNoTracking()
            .AnyAsync(
                follow => follow.FollowerMemberId == followerMemberId
                    && follow.FollowedMemberId == followedMemberId,
                cancellationToken);

    public async Task FollowAsync(
        Guid followerMemberId,
        Guid followedMemberId,
        DateTimeOffset followedAt,
        CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.MemberFollows.AnyAsync(
            follow => follow.FollowerMemberId == followerMemberId
                && follow.FollowedMemberId == followedMemberId,
            cancellationToken);
        if (exists)
        {
            return;
        }

        dbContext.MemberFollows.Add(new MemberFollowEntity
        {
            Id = Guid.NewGuid(),
            FollowerMemberId = followerMemberId,
            FollowedMemberId = followedMemberId,
            CreatedAt = followedAt,
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> UnfollowAsync(
        Guid followerMemberId,
        Guid followedMemberId,
        CancellationToken cancellationToken = default)
    {
        var deleted = await dbContext.MemberFollows
            .Where(follow => follow.FollowerMemberId == followerMemberId
                && follow.FollowedMemberId == followedMemberId)
            .ExecuteDeleteAsync(cancellationToken);
        return deleted > 0;
    }
}
