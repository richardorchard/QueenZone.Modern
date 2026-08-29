namespace QueenZone.Data;

public sealed class InMemoryAdminQueenHistoryRepository(SharedQueenHistoryStore store) : IAdminQueenHistoryRepository
{
    public Task<AdminQueenHistoryPage> GetPageAsync(
        AdminQueenHistoryListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(store.GetPage(filter, page, pageSize));

    public Task<QueenHistoryEvent?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(store.GetById(id));

    public Task<int> CreateAsync(AdminQueenHistoryDraft draft, CancellationToken cancellationToken = default) =>
        Task.FromResult(store.Create(draft));

    public Task UpdateAsync(int id, AdminQueenHistoryDraft draft, CancellationToken cancellationToken = default)
    {
        if (!store.Update(id, draft))
        {
            throw new InvalidOperationException($"Queen history event {id} was not found.");
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (!store.Delete(id))
        {
            throw new InvalidOperationException($"Queen history event {id} was not found.");
        }

        return Task.CompletedTask;
    }

    public Task SetPublishedAsync(int id, bool isPublished, CancellationToken cancellationToken = default)
    {
        if (!store.SetPublished(id, isPublished))
        {
            throw new InvalidOperationException($"Queen history event {id} was not found.");
        }

        return Task.CompletedTask;
    }
}
