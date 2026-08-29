using Microsoft.EntityFrameworkCore;
using QueenZone.Data.Entities;

namespace QueenZone.Data;

public sealed class EfAdminQueenHistoryRepository(QueenZoneDbContext dbContext) : IAdminQueenHistoryRepository
{
    public async Task<AdminQueenHistoryPage> GetPageAsync(
        AdminQueenHistoryListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.QueenHistoryEvents.AsNoTracking().AsQueryable();
        if (filter.IsPublished is bool isPublished)
        {
            query = query.Where(item => item.IsPublished == isPublished);
        }

        if (!string.IsNullOrWhiteSpace(filter.Query))
        {
            var needle = filter.Query.Trim();
            query = query.Where(item => item.Title.Contains(needle) || item.Summary.Contains(needle));
        }

        var rows = await query.ToListAsync(cancellationToken);
        var sorted = rows
            .OrderByDescending(item => item.EventDate)
            .ThenByDescending(item => item.Importance)
            .ThenBy(item => item.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var safePage = Math.Max(page, 1);
        var safePageSize = Math.Clamp(pageSize, 1, 100);
        var items = sorted
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .Select(Map)
            .ToList();

        return new AdminQueenHistoryPage(items, sorted.Count, safePage, safePageSize);
    }

    public async Task<QueenHistoryEvent?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var row = await dbContext.QueenHistoryEvents
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        return row is null ? null : Map(row);
    }

    public async Task<int> CreateAsync(AdminQueenHistoryDraft draft, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var row = new QueenHistoryEventEntity
        {
            Title = draft.Title,
            Summary = draft.Summary,
            EventDate = draft.EventDate,
            DatePrecision = draft.DatePrecision,
            Category = draft.Category,
            Importance = draft.Importance,
            SourceType = QueenHistoryEventSourceType.Curated,
            SourceKey = $"curated:{Guid.NewGuid():N}",
            SourceUrl = draft.SourceUrl,
            IsPublished = draft.IsPublished,
            CreatedAt = now,
            UpdatedAt = now,
        };

        dbContext.QueenHistoryEvents.Add(row);
        await dbContext.SaveChangesAsync(cancellationToken);
        return row.Id;
    }

    public async Task UpdateAsync(int id, AdminQueenHistoryDraft draft, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var updated = await dbContext.QueenHistoryEvents
            .Where(item => item.Id == id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(item => item.Title, draft.Title)
                    .SetProperty(item => item.Summary, draft.Summary)
                    .SetProperty(item => item.EventDate, draft.EventDate)
                    .SetProperty(item => item.DatePrecision, draft.DatePrecision)
                    .SetProperty(item => item.Category, draft.Category)
                    .SetProperty(item => item.Importance, draft.Importance)
                    .SetProperty(item => item.SourceUrl, draft.SourceUrl)
                    .SetProperty(item => item.IsPublished, draft.IsPublished)
                    .SetProperty(item => item.UpdatedAt, now),
                cancellationToken);

        if (updated == 0)
        {
            throw new InvalidOperationException($"Queen history event {id} was not found.");
        }
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var deleted = await dbContext.QueenHistoryEvents
            .Where(item => item.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        if (deleted == 0)
        {
            throw new InvalidOperationException($"Queen history event {id} was not found.");
        }
    }

    public async Task SetPublishedAsync(int id, bool isPublished, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var updated = await dbContext.QueenHistoryEvents
            .Where(item => item.Id == id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(item => item.IsPublished, isPublished)
                    .SetProperty(item => item.UpdatedAt, now),
                cancellationToken);

        if (updated == 0)
        {
            throw new InvalidOperationException($"Queen history event {id} was not found.");
        }
    }

    private static QueenHistoryEvent Map(QueenHistoryEventEntity row) =>
        new(
            row.Id,
            row.Title,
            row.Summary,
            row.EventDate,
            row.DatePrecision,
            row.Category,
            row.Importance,
            row.SourceType,
            row.SourceKey,
            row.SourceUrl,
            row.IsPublished);
}
