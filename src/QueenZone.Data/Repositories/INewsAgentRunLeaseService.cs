namespace QueenZone.Data;

public interface INewsAgentRunLeaseService
{
    Task<INewsAgentRunLease?> TryAcquireAsync(
        string leaseName,
        TimeSpan duration,
        CancellationToken cancellationToken = default);
}

public interface INewsAgentRunLease : IAsyncDisposable
{
    string LeaseName { get; }

    string HolderId { get; }
}
