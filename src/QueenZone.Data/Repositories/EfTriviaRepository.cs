using Microsoft.EntityFrameworkCore;
using QueenZone.Data.Entities;

namespace QueenZone.Data;

public sealed class EfTriviaRepository(QueenZoneDbContext dbContext) : ITriviaRepository
{
    public async Task<IReadOnlyList<TriviaFactItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.TriviaFacts
            .AsNoTracking()
            .OrderByDescending(row => row.CreatedAt)
            .ThenByDescending(row => row.Id)
            .ToListAsync(cancellationToken);

        return rows.Select(Map).ToList();
    }

    public async Task<TriviaFactItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var row = await dbContext.TriviaFacts
            .AsNoTracking()
            .SingleOrDefaultAsync(fact => fact.Id == id, cancellationToken);

        return row is null ? null : Map(row);
    }

    public async Task<TriviaFactItem?> GetRandomPublishedAsync(CancellationToken cancellationToken = default)
    {
        var publishedIds = await dbContext.TriviaFacts
            .AsNoTracking()
            .Where(fact => fact.IsPublished)
            .Select(fact => fact.Id)
            .ToListAsync(cancellationToken);

        if (publishedIds.Count == 0)
        {
            return null;
        }

        var chosenId = publishedIds[Random.Shared.Next(publishedIds.Count)];
        return await GetByIdAsync(chosenId, cancellationToken);
    }

    public async Task<int> CreateAsync(AdminTriviaDraft draft, CancellationToken cancellationToken = default)
    {
        var row = new TriviaFactEntity
        {
            Text = draft.Text,
            Category = draft.Category,
            Difficulty = draft.Difficulty,
            Source = draft.Source,
            CreatedAt = DateTime.UtcNow,
            IsPublished = draft.IsPublished,
        };

        dbContext.TriviaFacts.Add(row);
        await dbContext.SaveChangesAsync(cancellationToken);
        return row.Id;
    }

    public async Task UpdateAsync(int id, AdminTriviaDraft draft, CancellationToken cancellationToken = default)
    {
        var updated = await dbContext.TriviaFacts
            .Where(fact => fact.Id == id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(fact => fact.Text, draft.Text)
                    .SetProperty(fact => fact.Category, draft.Category)
                    .SetProperty(fact => fact.Difficulty, draft.Difficulty)
                    .SetProperty(fact => fact.Source, draft.Source)
                    .SetProperty(fact => fact.IsPublished, draft.IsPublished),
                cancellationToken);

        if (updated == 0)
        {
            throw new InvalidOperationException($"Trivia fact {id} was not found.");
        }
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var deleted = await dbContext.TriviaFacts
            .Where(fact => fact.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        if (deleted == 0)
        {
            throw new InvalidOperationException($"Trivia fact {id} was not found.");
        }
    }

    public async Task SetPublishedAsync(int id, bool isPublished, CancellationToken cancellationToken = default)
    {
        var updated = await dbContext.TriviaFacts
            .Where(fact => fact.Id == id)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(fact => fact.IsPublished, isPublished),
                cancellationToken);

        if (updated == 0)
        {
            throw new InvalidOperationException($"Trivia fact {id} was not found.");
        }
    }

    private static TriviaFactItem Map(TriviaFactEntity row) =>
        new(row.Id, row.Text, row.CreatedAt, row.IsPublished, row.Category, row.Difficulty, row.Source);
}
