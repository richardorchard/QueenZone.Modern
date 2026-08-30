namespace QueenZone.Data;

public interface ITriviaRepository
{
    Task<IReadOnlyList<TriviaFactItem>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<TriviaFactItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<TriviaFactItem?> GetRandomPublishedAsync(CancellationToken cancellationToken = default);

    Task<int> CreateAsync(AdminTriviaDraft draft, CancellationToken cancellationToken = default);

    Task UpdateAsync(int id, AdminTriviaDraft draft, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task SetPublishedAsync(int id, bool isPublished, CancellationToken cancellationToken = default);
}
