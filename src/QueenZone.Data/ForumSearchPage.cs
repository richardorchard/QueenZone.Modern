namespace QueenZone.Data;

public sealed record ForumSearchPage(
    IReadOnlyList<ForumSearchResult> Results,
    int TotalCount,
    int Page,
    int PageSize);
