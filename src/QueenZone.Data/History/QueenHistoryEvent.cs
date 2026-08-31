namespace QueenZone.Data;

public sealed record QueenHistoryEvent(
    int Id,
    string Title,
    string Summary,
    DateTime EventDate,
    QueenHistoryDatePrecision DatePrecision,
    QueenHistoryEventCategory Category,
    int Importance,
    QueenHistoryEventSourceType SourceType,
    string SourceKey,
    string? SourceUrl,
    bool IsPublished,
    byte[]? RowVersion = null)
{
    public string FormattedDate => EventDate.ToString("dd MMM yyyy", System.Globalization.CultureInfo.InvariantCulture);
}
