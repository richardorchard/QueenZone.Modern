namespace QueenZone.Data;

public interface IFanPerformanceRepository
{
    Task<IReadOnlyList<FanPerformance>> GetPageAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    Task<int> GetVisibleCountAsync(CancellationToken cancellationToken = default);

    Task<FanPerformance?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
