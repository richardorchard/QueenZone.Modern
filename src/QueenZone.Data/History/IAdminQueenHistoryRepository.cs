namespace QueenZone.Data;

public interface IAdminQueenHistoryRepository
{
    Task<AdminQueenHistoryPage> GetPageAsync(
        AdminQueenHistoryListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<QueenHistoryEvent?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<int> CreateAsync(AdminQueenHistoryDraft draft, CancellationToken cancellationToken = default);

    Task UpdateAsync(
        int id,
        AdminQueenHistoryDraft draft,
        byte[]? expectedRowVersion = null,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, byte[]? expectedRowVersion = null, CancellationToken cancellationToken = default);

    Task SetPublishedAsync(
        int id,
        bool isPublished,
        byte[]? expectedRowVersion = null,
        CancellationToken cancellationToken = default);
}
