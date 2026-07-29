namespace QueenZone.Web.Archive;

/// <summary>
/// Shared archive pagination loader used by news, articles, and future content-type page models.
/// Handles page-out-of-range guards and stale-count correction via the caller-supplied
/// <paramref name="resolveTotalPages"/> delegate.
/// </summary>
public static class ArchivePageLoader
{
    /// <summary>
    /// Loads one page of archive items and returns a typed result.
    /// </summary>
    /// <typeparam name="T">The raw repository item type.</typeparam>
    /// <param name="page">The requested page number (1-based).</param>
    /// <param name="pageSize">Items per page.</param>
    /// <param name="getCount">Returns the total published count (typically from a cache).</param>
    /// <param name="getPage">Returns the items for a given page and page size.</param>
    /// <param name="resolveTotalPages">
    ///   Corrects a stale cached count; receives (page, itemCount, publishedCount, rawTotalPages).
    ///   Use <see cref="ArchivePagination.ResolveTotalPages"/> for news/articles, or a pass-through
    ///   lambda <c>(_, _, _, tp) =&gt; tp</c> when the count comes from the same query as the page.
    /// </param>
    /// <param name="buildContext">Builds the <see cref="ArchivePageContext"/> from (currentPage, totalPages).</param>
    /// <param name="ct">Cancellation token.</param>
    public static async Task<ArchivePageResult<T>> LoadAsync<T>(
        int page,
        int pageSize,
        Func<CancellationToken, Task<int>> getCount,
        Func<int, int, CancellationToken, Task<IReadOnlyList<T>>> getPage,
        Func<int, int, int, int, int> resolveTotalPages,
        Func<int, int, ArchivePageContext> buildContext,
        CancellationToken ct)
    {
        if (page < 1)
            return new ArchivePageResult<T>.NotFound();

        var publishedCount = await getCount(ct);
        var items = await getPage(page, pageSize, ct);
        var rawTotalPages = ArchivePagination.GetTotalPages(publishedCount, pageSize);
        var totalPages = resolveTotalPages(page, items.Count, publishedCount, rawTotalPages);

        if (totalPages == 0)
        {
            if (page > 1)
                return new ArchivePageResult<T>.NotFound();
        }
        else if (page > totalPages)
        {
            return new ArchivePageResult<T>.NotFound();
        }

        return new ArchivePageResult<T>.Success
        {
            Items = items,
            Context = buildContext(page, totalPages),
        };
    }
}
