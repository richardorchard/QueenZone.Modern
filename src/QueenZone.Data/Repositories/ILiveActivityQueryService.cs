namespace QueenZone.Data;

/// <summary>
/// Backs the mobile home screen's "live activity" strip. No member presence/heartbeat
/// tracking exists in this codebase, so the only honest live signal is a count of forum
/// replies posted today.
/// </summary>
public interface ILiveActivityQueryService
{
    Task<int> GetNewForumRepliesTodayAsync(CancellationToken cancellationToken = default);
}
