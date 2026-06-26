namespace QueenZone.Web;

/// <summary>
/// Shared pagination math and pagination-nav view-model building used by the
/// News, Articles, and Forum archive/category/topic pages. Rendering happens
/// in the <c>_ArchivePagination</c> Razor partial.
/// </summary>
internal static class ArchivePagination
{
    public static int GetTotalPages(int totalCount, int pageSize)
    {
        if (totalCount <= 0)
        {
            return 0;
        }

        return (totalCount + pageSize - 1) / pageSize;
    }

    public static int ResolveTotalPages(int currentPage, int itemCount, int publishedCount, int totalPages, int pageSize)
    {
        if (itemCount == pageSize && totalPages <= currentPage)
        {
            return Math.Max(totalPages, currentPage + 1);
        }

        if (publishedCount > 0)
        {
            return totalPages;
        }

        return itemCount == 0 ? 0 : Math.Max(totalPages, currentPage);
    }

    public static ArchivePaginationViewModel? BuildViewModel(
        string ariaLabel,
        int currentPage,
        int totalPages,
        Func<int, string> pageHref)
    {
        if (totalPages <= 1)
        {
            return null;
        }

        var pages = new List<ArchivePaginationPageLink>();

        foreach (var pageNumber in GetVisiblePageNumbers(currentPage, totalPages))
        {
            if (pageNumber is null)
            {
                pages.Add(new ArchivePaginationPageLink());
            }
            else if (pageNumber == currentPage)
            {
                pages.Add(new ArchivePaginationPageLink { PageNumber = pageNumber, IsCurrent = true });
            }
            else
            {
                pages.Add(new ArchivePaginationPageLink { PageNumber = pageNumber, Href = pageHref(pageNumber.Value) });
            }
        }

        return new ArchivePaginationViewModel
        {
            AriaLabel = ariaLabel,
            CurrentPage = currentPage,
            TotalPages = totalPages,
            PreviousHref = currentPage > 1 ? pageHref(currentPage - 1) : null,
            NextHref = currentPage < totalPages ? pageHref(currentPage + 1) : null,
            Pages = pages,
        };
    }

    private static IEnumerable<int?> GetVisiblePageNumbers(int currentPage, int totalPages)
    {
        if (totalPages <= 7)
        {
            for (var page = 1; page <= totalPages; page++)
            {
                yield return page;
            }

            yield break;
        }

        yield return 1;

        if (currentPage > 3)
        {
            yield return null;
        }

        var start = Math.Max(2, currentPage - 1);
        var end = Math.Min(totalPages - 1, currentPage + 1);
        for (var page = start; page <= end; page++)
        {
            yield return page;
        }

        if (currentPage < totalPages - 2)
        {
            yield return null;
        }

        yield return totalPages;
    }
}
