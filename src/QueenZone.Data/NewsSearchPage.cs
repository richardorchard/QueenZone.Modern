namespace QueenZone.Data;

/// <summary>
/// Paginated result set for a published news keyword search.
/// </summary>
public sealed record NewsSearchPage(
    IReadOnlyList<NewsItem> Items,
    int TotalCount,
    int Page,
    int PageSize);
