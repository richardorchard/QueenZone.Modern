namespace QueenZone.Data;

public interface ISearchReindexRunLeaseService
{
    Task<ISearchReindexRunLease?> TryAcquireAsync(
        string leaseName,
        TimeSpan duration,
        CancellationToken cancellationToken = default);
}

public interface ISearchReindexRunLease : IAsyncDisposable
{
    string LeaseName { get; }

    string HolderId { get; }
}
