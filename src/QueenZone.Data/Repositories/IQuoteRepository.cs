namespace QueenZone.Data;

public interface IQuoteRepository
{
    Task<IReadOnlyList<QuoteItem>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<QuoteItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<QuoteItem?> GetRandomPublishedAsync(CancellationToken cancellationToken = default);

    Task<int> CreateAsync(AdminQuoteDraft draft, CancellationToken cancellationToken = default);

    Task UpdateAsync(int id, AdminQuoteDraft draft, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task SetPublishedAsync(int id, bool isPublished, CancellationToken cancellationToken = default);
}
