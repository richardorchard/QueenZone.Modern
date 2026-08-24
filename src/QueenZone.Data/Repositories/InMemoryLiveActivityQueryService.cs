namespace QueenZone.Data;

/// <summary>
/// Sample-data implementation for the Testing/E2E/in-memory-data hosts, which have no
/// <see cref="QueenZoneDbContext"/> registered. Returns a small fixed count so the mobile
/// home screen's live-activity strip has something deterministic to render against sample
/// data.
/// </summary>
public sealed class InMemoryLiveActivityQueryService : ILiveActivityQueryService
{
    public Task<int> GetNewForumRepliesTodayAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(3);
}
