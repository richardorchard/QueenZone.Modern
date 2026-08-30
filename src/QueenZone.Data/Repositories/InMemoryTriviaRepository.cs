namespace QueenZone.Data;

public sealed class InMemoryTriviaRepository(SharedTriviaStore store) : ITriviaRepository
{
    public InMemoryTriviaRepository(IReadOnlyList<TriviaFactItem> seedFacts)
        : this(new SharedTriviaStore(seedFacts))
    {
    }

    public Task<IReadOnlyList<TriviaFactItem>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(store.GetAll());

    public Task<TriviaFactItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(store.GetById(id));

    public Task<TriviaFactItem?> GetRandomPublishedAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(store.GetRandomPublished());

    public Task<int> CreateAsync(AdminTriviaDraft draft, CancellationToken cancellationToken = default) =>
        Task.FromResult(store.Create(draft));

    public Task UpdateAsync(int id, AdminTriviaDraft draft, CancellationToken cancellationToken = default)
    {
        if (!store.Update(id, draft))
        {
            throw new InvalidOperationException($"Trivia fact {id} was not found.");
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (!store.Delete(id))
        {
            throw new InvalidOperationException($"Trivia fact {id} was not found.");
        }

        return Task.CompletedTask;
    }

    public Task SetPublishedAsync(int id, bool isPublished, CancellationToken cancellationToken = default)
    {
        if (!store.SetPublished(id, isPublished))
        {
            throw new InvalidOperationException($"Trivia fact {id} was not found.");
        }

        return Task.CompletedTask;
    }
}
