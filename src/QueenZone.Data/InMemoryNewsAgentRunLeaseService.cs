namespace QueenZone.Data;

public sealed class InMemoryNewsAgentRunLeaseService(SharedNewsAgentLeaseStore store) : INewsAgentRunLeaseService
{
    public Task<INewsAgentRunLease?> TryAcquireAsync(
        string leaseName,
        TimeSpan duration,
        CancellationToken cancellationToken = default)
    {
        var holderId = Guid.NewGuid().ToString("N");
        var expiresAtUtc = DateTime.UtcNow.Add(duration);
        if (!store.TryAcquire(leaseName, holderId, expiresAtUtc))
        {
            return Task.FromResult<INewsAgentRunLease?>(null);
        }

        return Task.FromResult<INewsAgentRunLease?>(new InMemoryNewsAgentRunLease(store, leaseName, holderId));
    }

    private sealed class InMemoryNewsAgentRunLease(
        SharedNewsAgentLeaseStore store,
        string leaseName,
        string holderId) : INewsAgentRunLease
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
