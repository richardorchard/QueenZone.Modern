namespace QueenZone.Data;

public sealed class InMemoryAdminNewsRepository(SharedNewsStore store) : IAdminNewsRepository
{
    public Task<IReadOnlyList<AdminNewsArticle>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(store.GetAllArticles());

    public Task<AdminNewsArticle?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(store.GetArticle(id));

    public Task<int> CreateDraftAsync(AdminNewsDraft draft, string editorEmail, CancellationToken cancellationToken = default) =>
        Task.FromResult(store.CreateDraft(draft, editorEmail));

    public Task UpdateAsync(int id, AdminNewsDraft draft, string editorEmail, CancellationToken cancellationToken = default)
    {
        if (!store.Update(id, draft, editorEmail))
        {
            throw new InvalidOperationException($"News article {id} was not found.");
        }

        return Task.CompletedTask;
    }

    public Task PublishAsync(int id, string editorEmail, CancellationToken cancellationToken = default)
    {
        if (!store.SetPublished(id, true, editorEmail))
        {
            throw new InvalidOperationException($"News article {id} was not found.");
        }

        return Task.CompletedTask;
    }

    public Task UnpublishAsync(int id, string editorEmail, CancellationToken cancellationToken = default)
    {
        if (!store.SetPublished(id, false, editorEmail))
        {
            throw new InvalidOperationException($"News article {id} was not found.");
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(int id, string editorEmail, CancellationToken cancellationToken = default)
    {
        if (!store.Delete(id))
        {
            throw new InvalidOperationException($"News article {id} was not found.");
        }

        return Task.CompletedTask;
    }

    public Task<bool> IsSlugInUseAsync(string slug, int? excludeNewsId = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(store.IsSlugInUse(slug, excludeNewsId));
}