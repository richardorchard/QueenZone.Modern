namespace QueenZone.Data;

public sealed class InMemoryQueenHistoryRepository(IReadOnlyList<QueenHistoryEvent> events) : IQueenHistoryRepository
{
    public Task<IReadOnlyList<QueenHistoryEvent>> GetOnThisDayAsync(
        DateOnly date,
        int count,
        CancellationToken cancellationToken = default)
    {
        var matches = events.Where(item =>
            item.EventDate.Month == date.Month &&
            item.EventDate.Day == date.Day);

        return Task.FromResult(QueenHistoryEventOrdering.ForHomepage(matches, count));
    }

    public Task<IReadOnlyList<QueenHistoryEvent>> GetAroundThisDayAsync(
        DateOnly date,
        int dayWindow,
        int count,
        CancellationToken cancellationToken = default)
    {
        var matches = events
            .Where(item => item.IsPublished && item.DatePrecision == QueenHistoryDatePrecision.ExactDate)
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

        return Task.FromResult<IReadOnlyList<QueenHistoryEvent>>(matches);
    }
}
