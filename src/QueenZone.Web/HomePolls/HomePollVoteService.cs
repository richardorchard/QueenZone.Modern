using Microsoft.AspNetCore.OutputCaching;
using QueenZone.Data;

namespace QueenZone.Web;

/// <summary>
/// Single write path for Home poll ballots. Website Index POST and
/// <c>POST /api/v1/content/home-poll/votes</c> both call this service.
/// </summary>
public sealed class HomePollVoteService(
    IHomePollRepository homePollRepository,
    IMemberAccountRepository memberAccountRepository,
    IOutputCacheStore outputCacheStore)
{
    public async Task CastVoteAsync(Guid memberId, Guid optionId, CancellationToken cancellationToken)
    {
        var account = await memberAccountRepository.FindByIdAsync(memberId, cancellationToken);
        if (account?.IsSuspended == true)
        {
            throw new ForumPollVoteException(
                ForumPollVoteException.Forbidden,
                ForumPostWriteService.SuspendedMessage);
        }

        await homePollRepository.CastVoteAsync(optionId, memberId, cancellationToken);
        await outputCacheStore.EvictByTagAsync(PublicOutputCachePolicies.PublicHtmlTag, cancellationToken);
    }
}
