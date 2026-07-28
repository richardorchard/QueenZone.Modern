namespace QueenZone.Data;

public sealed record AdminFreddieTributeListFilter(bool? IsVisible, string? Search, bool DuplicatesOnly);

public sealed record AdminFreddieTributeItem(
    int Id,
    string Name,
    string Thought,
    string? Country,
    string DateText,
    string? TimeText,
    bool IsVisible,
    int DuplicateCount);

public sealed record AdminFreddieTributePage(
    IReadOnlyList<AdminFreddieTributeItem> Items,
    int TotalCount,
    int Page,
    int PageSize);

