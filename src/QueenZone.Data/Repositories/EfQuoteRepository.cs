using Microsoft.EntityFrameworkCore;
using QueenZone.Data.Entities;

namespace QueenZone.Data;

public sealed class EfQuoteRepository(QueenZoneDbContext dbContext) : IQuoteRepository
{
    public async Task<IReadOnlyList<QuoteItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.Quotes
            .AsNoTracking()
            .OrderByDescending(row => row.CreatedAt)
            .ThenByDescending(row => row.QuoteId)
            .ToListAsync(cancellationToken);

        return rows.Select(Map).ToList();
    }

    public async Task<QuoteItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var row = await dbContext.Quotes
            .AsNoTracking()
            .SingleOrDefaultAsync(quote => quote.QuoteId == id, cancellationToken);

        return row is null ? null : Map(row);
    }

    public async Task<QuoteItem?> GetRandomPublishedAsync(CancellationToken cancellationToken = default)
    {
        // Table is small (dozens-to-low-hundreds of rows): pull published ids and pick
        // client-side rather than relying on a provider-specific random ORDER BY.
        var publishedIds = await dbContext.Quotes
            .AsNoTracking()
            .Where(quote => quote.IsPublished)
            .Select(quote => quote.QuoteId)
            .ToListAsync(cancellationToken);

        if (publishedIds.Count == 0)
        {
            return null;
        }

        var chosenId = publishedIds[Random.Shared.Next(publishedIds.Count)];
        return await GetByIdAsync(chosenId, cancellationToken);
    }

    public async Task<int> CreateAsync(AdminQuoteDraft draft, CancellationToken cancellationToken = default)
    {
        var row = new QuoteEntity
        {
            Text = draft.Text,
            WhoSaid = draft.WhoSaid,
            Context = draft.Context,
            CreatedAt = DateTime.UtcNow,
            IsPublished = draft.IsPublished,
        };

        dbContext.Quotes.Add(row);
        await dbContext.SaveChangesAsync(cancellationToken);
        return row.QuoteId;
    }

    public async Task UpdateAsync(int id, AdminQuoteDraft draft, CancellationToken cancellationToken = default)
    {
        var updated = await dbContext.Quotes
            .Where(quote => quote.QuoteId == id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(quote => quote.Text, draft.Text)
                    .SetProperty(quote => quote.WhoSaid, draft.WhoSaid)
                    .SetProperty(quote => quote.Context, draft.Context)
                    .SetProperty(quote => quote.IsPublished, draft.IsPublished),
                cancellationToken);

        if (updated == 0)
        {
            throw new InvalidOperationException($"Quote {id} was not found.");
        }
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var deleted = await dbContext.Quotes
            .Where(quote => quote.QuoteId == id)
            .ExecuteDeleteAsync(cancellationToken);

        if (deleted == 0)
        {
            throw new InvalidOperationException($"Quote {id} was not found.");
        }
    }

    public async Task SetPublishedAsync(int id, bool isPublished, CancellationToken cancellationToken = default)
    {
        var updated = await dbContext.Quotes
            .Where(quote => quote.QuoteId == id)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(quote => quote.IsPublished, isPublished),
                cancellationToken);

        if (updated == 0)
        {
            throw new InvalidOperationException($"Quote {id} was not found.");
        }
    }

    private static QuoteItem Map(QuoteEntity row) =>
        new(row.QuoteId, row.Text, row.WhoSaid, row.CreatedAt, row.IsPublished, row.Context);
}
