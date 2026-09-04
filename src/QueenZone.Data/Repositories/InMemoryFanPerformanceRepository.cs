namespace QueenZone.Data;

public sealed class InMemoryFanPerformanceRepository : IFanPerformanceRepository
{
    private readonly SharedFanPerformanceStore store;

    public InMemoryFanPerformanceRepository(IReadOnlyList<FanPerformance> performances)
        : this(new SharedFanPerformanceStore(performances))
    {
    }

    public InMemoryFanPerformanceRepository(SharedFanPerformanceStore store)
    {
        this.store = store;
    }

    public Task<IReadOnlyList<FanPerformance>> GetPageAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var visible = store.GetVisible();
        IReadOnlyList<FanPerformance> paged = visible
            .Skip(Math.Max(page - 1, 0) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult(paged);
    }

    public Task<int> GetVisibleCountAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(store.GetVisible().Count);

    public Task<FanPerformance?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(store.GetVisibleById(id));
}
