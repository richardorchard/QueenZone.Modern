namespace QueenZone.Data;

public sealed class InMemoryQuoteRepository(SharedQuoteStore store) : IQuoteRepository
{
    public InMemoryQuoteRepository(IReadOnlyList<QuoteItem> seedQuotes)
        : this(new SharedQuoteStore(seedQuotes))
    {
    }

    public Task<IReadOnlyList<QuoteItem>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(store.GetAll());

    public Task<QuoteItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(store.GetById(id));

    public Task<QuoteItem?> GetRandomPublishedAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(store.GetRandomPublished());

    public Task<int> CreateAsync(AdminQuoteDraft draft, CancellationToken cancellationToken = default) =>
        Task.FromResult(store.Create(draft));

    public Task UpdateAsync(int id, AdminQuoteDraft draft, CancellationToken cancellationToken = default)
    {
        if (!store.Update(id, draft))
        {
            throw new InvalidOperationException($"Quote {id} was not found.");
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (!store.Delete(id))
        {
            throw new InvalidOperationException($"Quote {id} was not found.");
        }

        return Task.CompletedTask;
    }

    public Task SetPublishedAsync(int id, bool isPublished, CancellationToken cancellationToken = default)
    {
        if (!store.SetPublished(id, isPublished))
        {
            throw new InvalidOperationException($"Quote {id} was not found.");
        }

        return Task.CompletedTask;
    }
}
