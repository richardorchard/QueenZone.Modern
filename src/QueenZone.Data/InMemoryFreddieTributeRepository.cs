namespace QueenZone.Data;

public sealed class InMemoryFreddieTributeRepository(IReadOnlyList<FreddieTribute> tributes) : IFreddieTributeRepository
{
    private readonly IReadOnlyList<FreddieTribute> visibleTributes = tributes
        .Where(tribute => !string.IsNullOrWhiteSpace(tribute.Thought))
        .OrderByDescending(tribute => tribute.Id)
        .ToList();

    public Task<FreddieTributePage> GetPageAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var safePage = Math.Max(page, 1);
        var safePageSize = Math.Clamp(pageSize, 1, 100);
        var items = visibleTributes
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToList();

        return Task.FromResult(new FreddieTributePage(items, visibleTributes.Count));
    }

    public Task<FreddieTribute?> GetRandomAsync(CancellationToken cancellationToken = default)
    {
        if (visibleTributes.Count == 0)
        {
            return Task.FromResult<FreddieTribute?>(null);
        }

        var index = Random.Shared.Next(visibleTributes.Count);
        return Task.FromResult<FreddieTribute?>(visibleTributes[index]);
    }
}

