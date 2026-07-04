using Microsoft.EntityFrameworkCore;

namespace QueenZone.Data;

public sealed class EfQueenHistoryRepository(QueenZoneDbContext dbContext) : IQueenHistoryRepository
{
    public async Task<IReadOnlyList<QueenHistoryEvent>> GetOnThisDayAsync(
        DateOnly date,
        int count,
        CancellationToken cancellationToken = default)
    {
        var events = await GetPublishedExactEventsAsync(cancellationToken);
        var matches = events.Where(item =>
            item.EventDate.Month == date.Month &&
            item.EventDate.Day == date.Day);

        return QueenHistoryEventOrdering.ForHomepage(matches, count);
    }

    public async Task<IReadOnlyList<QueenHistoryEvent>> GetAroundThisDayAsync(
        DateOnly date,
        int dayWindow,
        int count,
        CancellationToken cancellationToken = default)
    {
        var events = await GetPublishedExactEventsAsync(cancellationToken);
        var matches = events
            .Select(item => new
            {
                Event = item,
                Distance = QueenHistoryEventOrdering.DayDistance(date, item.EventDate),
            })
            .Where(item => item.Distance > 0 && item.Distance <= dayWindow)
            .OrderBy(item => item.Distance)
            .ThenByDescending(item => item.Event.Importance)
            .ThenBy(item => item.Event.EventDate.Year)
            .ThenBy(item => item.Event.Title, StringComparer.OrdinalIgnoreCase)
            .Select(item => item.Event)
            .Take(Math.Max(count, 0))
            .ToList();

        return matches;
    }

    private async Task<IReadOnlyList<QueenHistoryEvent>> GetPublishedExactEventsAsync(CancellationToken cancellationToken) =>
        await dbContext.QueenHistoryEvents
            .AsNoTracking()
            .Where(item => item.IsPublished && item.DatePrecision == QueenHistoryDatePrecision.ExactDate)
            .Select(item => new QueenHistoryEvent(
                item.Id,
                item.Title,
                item.Summary,
                item.EventDate,
                item.DatePrecision,
                item.Category,
                item.Importance,
                item.SourceType,
                item.SourceKey,
                item.SourceUrl,
                item.IsPublished))
            .ToListAsync(cancellationToken);
}
