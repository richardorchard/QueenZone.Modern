namespace QueenZone.Data;

public sealed class InMemorySearchReindexRunLeaseService(SharedSearchReindexLeaseStore store) : ISearchReindexRunLeaseService
{
    public Task<ISearchReindexRunLease?> TryAcquireAsync(
        string leaseName,
        TimeSpan duration,
        CancellationToken cancellationToken = default)
    {
        var holderId = Guid.NewGuid().ToString("N");
        var expiresAtUtc = DateTime.UtcNow.Add(duration);
        if (!store.TryAcquire(leaseName, holderId, expiresAtUtc))
        {
            return Task.FromResult<ISearchReindexRunLease?>(null);
        }

        return Task.FromResult<ISearchReindexRunLease?>(new InMemorySearchReindexRunLease(store, leaseName, holderId));
    }

    private sealed class InMemorySearchReindexRunLease(
        SharedSearchReindexLeaseStore store,
        string leaseName,
        string holderId) : ISearchReindexRunLease
    {
        public string LeaseName { get; } = leaseName;

        public string HolderId { get; } = holderId;

        public ValueTask DisposeAsync()
        {
            store.Release(LeaseName, HolderId);
            return ValueTask.CompletedTask;
        }
    }
}
