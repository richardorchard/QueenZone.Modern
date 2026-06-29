namespace QueenZone.Data;

public sealed class InMemoryFanPerformanceRepository : IFanPerformanceRepository
{
    private readonly IReadOnlyList<FanPerformance> performances;

    public InMemoryFanPerformanceRepository(IReadOnlyList<FanPerformance> performances)
    {
        this.performances = performances;
    }

    public Task<IReadOnlyList<FanPerformance>> GetPageAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<FanPerformance> paged = performances
            .OrderByDescending(performance => performance.DateAdded)
            .Skip(Math.Max(page - 1, 0) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult(paged);
    }

    public Task<int> GetVisibleCountAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(performances.Count);
}
