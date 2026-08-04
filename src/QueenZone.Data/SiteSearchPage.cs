namespace QueenZone.Data;

/// <summary>
/// Globally ranked, paginated whole-site search results, optionally filtered to one content type.
/// </summary>
public sealed record SiteSearchPage(
    IReadOnlyList<SiteSearchResult> Results,
    int TotalCount,
    int Page,
    int PageSize);
