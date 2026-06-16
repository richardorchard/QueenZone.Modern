namespace QueenZone.Data;

public interface INewsAuditRepository
{
    Task AppendAsync(int newsId, string action, string actorEmail, string? details = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NewsAuditEntry>> GetByNewsIdAsync(int newsId, CancellationToken cancellationToken = default);
}