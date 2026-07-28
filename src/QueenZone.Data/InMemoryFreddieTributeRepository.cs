namespace QueenZone.Data;

public sealed class InMemoryFreddieTributeRepository(SharedFreddieTributeStore store) : IFreddieTributeRepository
{
    public Task<FreddieTributePage> GetPageAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(store.GetPublicPage(page, pageSize));

    public Task<FreddieTribute?> GetRandomAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(store.GetRandomPublicTribute());
}
