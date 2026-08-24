using QueenZone.Data;

namespace QueenZone.Web;

/// <summary>
/// Maps <see cref="SiteSearchResult"/> to <c>GET /api/v1/search</c> JSON.
/// Summaries stay plain text (website <c>&lt;mark&gt;</c> highlighting is Razor-only).
/// </summary>
public static class SearchApiMapper
{
    public static SearchResultDto ToItem(SiteSearchResult result) =>
        new(
            result.ContentType,
            result.SourceKey,
            result.Title,
            result.Summary,
            result.Url,
            result.PublishedAt,
            result.ImageUrl,
            result.Category,
            result.AuthorDisplayName,
            SearchDocumentSourceKey.TryParseNumericId(result.SourceKey));

    public static IReadOnlyList<SearchResultDto> ToItems(IEnumerable<SiteSearchResult> results) =>
        results.Select(ToItem).ToList();
}
