namespace QueenZone.Data;

/// <summary>
/// One search result, shaped for rendering a mixed-content-type result card. Deliberately has no
/// rank field — ranking happens server-side in <c>dbo.SearchDocument_Search</c> against the
/// shared full-text index, so results already arrive in ranked order.
/// </summary>
public sealed record SiteSearchResult(
    string ContentType,
    string Title,
    string Summary,
    string Url,
    DateTimeOffset? PublishedAt,
    string? ImageUrl,
    string? Category,
    string? AuthorDisplayName);
