namespace QueenZone.Data;

public sealed record AdminQueenHistoryDraft(
    string Title,
    string Summary,
    DateTime EventDate,
    QueenHistoryDatePrecision DatePrecision,
    QueenHistoryEventCategory Category,
    int Importance,
    string? SourceUrl,
    bool IsPublished);
