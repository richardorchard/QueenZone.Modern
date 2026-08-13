namespace QueenZone.Data;

public sealed record QueenHistoryCsvImportRow(
    string Title,
    string Summary,
    DateTime EventDate,
    QueenHistoryDatePrecision DatePrecision,
    QueenHistoryEventCategory Category,
    int Importance,
    QueenHistoryEventSourceType SourceType,
    string SourceKey,
    string? SourceUrl);
