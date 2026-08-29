namespace QueenZone.Data;

public sealed record AdminQueenHistoryPage(
    IReadOnlyList<QueenHistoryEvent> Items,
    int TotalCount,
    int Page,
    int PageSize);
