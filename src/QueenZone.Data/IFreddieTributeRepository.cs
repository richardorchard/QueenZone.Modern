namespace QueenZone.Data;

public interface IFreddieTributeRepository
{
    Task<FreddieTributePage> GetPageAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    Task<FreddieTribute?> GetRandomAsync(CancellationToken cancellationToken = default);
}

