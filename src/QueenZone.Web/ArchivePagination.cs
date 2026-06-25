using System.Net;
using System.Text;

namespace QueenZone.Web;

/// <summary>
/// Shared pagination math and pagination-nav HTML rendering used by the
/// News, Articles, and Forum archive/category/topic pages.
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

    public static string BuildNav(string ariaLabel, int currentPage, int totalPages, Func<int, string> pageHref)
    {
        if (totalPages <= 1)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        builder.Append("<nav class=\"archive-pagination\" aria-label=\"");
        builder.Append(WebUtility.HtmlEncode(ariaLabel));
        builder.Append("\">");
        builder.Append("<p class=\"archive-pagination-summary\">Page ");
        builder.Append(currentPage);
        builder.Append(" of ");
        builder.Append(totalPages);
        builder.Append("</p>");
        builder.Append("<div class=\"archive-pagination-controls\">");
        builder.Append(RenderPrevious(currentPage, pageHref));
        builder.Append("<ol class=\"archive-pagination-pages\">");

        foreach (var pageNumber in GetVisiblePageNumbers(currentPage, totalPages))
        {
            builder.Append("<li>");
            if (pageNumber is null)
            {
                builder.Append("<span class=\"archive-pagination-ellipsis\" aria-hidden=\"true\">…</span>");
            }
            else if (pageNumber == currentPage)
            {
                builder.Append("<span aria-current=\"page\">");
                builder.Append(pageNumber);
                builder.Append("</span>");
            }
            else
            {
                builder.Append("<a href=\"");
                builder.Append(WebUtility.HtmlEncode(pageHref(pageNumber.Value)));
                builder.Append("\">");
                builder.Append(pageNumber);
                builder.Append("</a>");
            }

            builder.Append("</li>");
        }

        builder.Append("</ol>");
        builder.Append(RenderNext(currentPage, totalPages, pageHref));
        builder.Append("</div></nav>");
        return builder.ToString();
    }

    private static string RenderPrevious(int currentPage, Func<int, string> pageHref)
    {
        if (currentPage <= 1)
        {
            return "<span class=\"archive-pagination-prev is-disabled\" aria-disabled=\"true\">Previous</span>";
        }

        var previousPath = WebUtility.HtmlEncode(pageHref(currentPage - 1));
        return $"<a class=\"archive-pagination-prev\" rel=\"prev\" href=\"{previousPath}\">Previous</a>";
    }

    private static string RenderNext(int currentPage, int totalPages, Func<int, string> pageHref)
    {
        if (currentPage >= totalPages)
        {
            return "<span class=\"archive-pagination-next is-disabled\" aria-disabled=\"true\">Next</span>";
        }

        var nextPath = WebUtility.HtmlEncode(pageHref(currentPage + 1));
        return $"<a class=\"archive-pagination-next\" rel=\"next\" href=\"{nextPath}\">Next</a>";
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
