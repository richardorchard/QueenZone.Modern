namespace QueenZone.Data;

/// <summary>
/// Caps for <c>dbo.SearchDocument_Search</c>. Common terms such as "queen" or "freddie"
/// otherwise score every <c>SearchDocument</c> row twice (page + count) and hit the 30-second
/// SQL command timeout, which the mobile client surfaces as "Unable to reach QueenZone".
/// </summary>
public static class SiteSearchLimits
{
    /// <summary>
    /// Rank-window size. Untyped (All) search applies this as <c>FREETEXTTABLE</c>
    /// <c>top_n_by_rank</c> so common terms stay inside the command timeout. Typed search
    /// filters <c>ContentType</c> first, then keeps this many hits. Pagination still works
    /// up to this many hits; <c>totalCount</c> is never larger.
    /// </summary>
    public const int MaxRankedMatches = 1000;
}
