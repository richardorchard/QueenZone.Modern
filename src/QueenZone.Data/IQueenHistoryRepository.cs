namespace QueenZone.Data;

public interface IQueenHistoryRepository
{
    Task<IReadOnlyList<QueenHistoryEvent>> GetOnThisDayAsync(
        DateOnly date,
        int count,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<QueenHistoryEvent>> GetAroundThisDayAsync(
        DateOnly date,
        int dayWindow,
        int count,
        CancellationToken cancellationToken = default);
}
