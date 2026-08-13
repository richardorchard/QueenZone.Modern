namespace QueenZone.Data;

internal static class QueenHistoryEventOrdering
{
    internal static IReadOnlyList<QueenHistoryEvent> ForHomepage(IEnumerable<QueenHistoryEvent> events, int count) =>
        events
            .Where(item => item.IsPublished && item.DatePrecision == QueenHistoryDatePrecision.ExactDate)
            .OrderByDescending(item => item.Importance)
            .ThenBy(item => item.EventDate.Year)
            .ThenBy(item => item.Title, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(count, 0))
            .ToList();

    internal static int DayDistance(DateOnly target, DateTime eventDate)
    {
        var day = eventDate.Month == 2 && eventDate.Day == 29 && !DateTime.IsLeapYear(target.Year)
            ? 28
            : eventDate.Day;
        var candidate = new DateOnly(target.Year, eventDate.Month, day);
        var distance = Math.Abs(candidate.DayNumber - target.DayNumber);
        return Math.Min(distance, 366 - distance);
    }
}
