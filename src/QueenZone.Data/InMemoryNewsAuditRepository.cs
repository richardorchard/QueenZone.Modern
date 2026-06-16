namespace QueenZone.Data;

public sealed class InMemoryNewsAuditRepository(SharedNewsStore store) : INewsAuditRepository
{
    public Task AppendAsync(
        int newsId,
        string action,
        string actorEmail,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        store.AppendAudit(newsId, action, actorEmail, details);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<NewsAuditEntry>> GetByNewsIdAsync(int newsId, CancellationToken cancellationToken = default) =>
        Task.FromResult(store.GetAuditEntries(newsId));
}