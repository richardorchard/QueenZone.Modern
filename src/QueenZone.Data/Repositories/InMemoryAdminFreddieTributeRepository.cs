namespace QueenZone.Data;

public sealed class InMemoryAdminFreddieTributeRepository(SharedFreddieTributeStore store) : IAdminFreddieTributeRepository
{
    public Task<AdminFreddieTributePage> GetPageAsync(
        AdminFreddieTributeListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(store.GetAdminPage(filter, page, pageSize));

    public Task<AdminFreddieTributeItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(store.GetById(id));

    public Task SetVisibilityAsync(
        int id,
        bool isVisible,
        string editorEmail,
        bool? expectedIsVisible = null,
        CancellationToken cancellationToken = default)
    {
        if (!store.SetVisibility(id, isVisible, expectedIsVisible))
        {
            throw new InvalidOperationException($"Freddie tribute {id} was not found.");
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(
        int id,
        string editorEmail,
        bool? expectedIsVisible = null,
        CancellationToken cancellationToken = default)
    {
        if (!store.Delete(id, expectedIsVisible))
        {
            throw new InvalidOperationException($"Freddie tribute {id} was not found.");
        }

        return Task.CompletedTask;
    }
}

