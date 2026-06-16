using System.Net;
using System.Text;
using QueenZone.Data;

namespace QueenZone.Web;

public static partial class NewsRoutes
{
    public const int ArchivePageSize = 20;

    public static int GetArchiveTotalPages(int publishedCount, int pageSize = ArchivePageSize)
    {
        if (publishedCount <= 0)
        {
            return 0;
        }

        return (publishedCount + pageSize - 1) / pageSize;
    }

    public static string GetArchiveCanonicalPath(int page) =>
        page <= 1 ? "/news" : $"/news/page/{page}";

    public static string GetArchivePageTitle(int page) =>
        page <= 1 ? "QueenZone news" : $"QueenZone news – Page {page}";

    public static string BuildArchivePaginationNav(int currentPage, int totalPages)
    {
        if (totalPages <= 1)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        builder.Append("<nav class=\"archive-pagination\" aria-label=\"News archive pagination\">");
        builder.Append("<p class=\"archive-pagination-summary\">Page ");
        builder.Append(currentPage);
        builder.Append(" of ");
        builder.Append(totalPages);
        builder.Append("</p>");
        builder.Append("<div class=\"archive-pagination-controls\">");
        builder.Append(RenderArchivePaginationPrevious(currentPage));
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
                builder.Append(WebUtility.HtmlEncode(GetArchiveCanonicalPath(pageNumber.Value)));
                builder.Append("\">");
                builder.Append(pageNumber);
                builder.Append("</a>");
            }

            builder.Append("</li>");
        }

        builder.Append("</ol>");
        builder.Append(RenderArchivePaginationNext(currentPage, totalPages));
        builder.Append("</div></nav>");
        return builder.ToString();
    }

    private static string RenderArchivePaginationPrevious(int currentPage)
    {
        if (currentPage <= 1)
        {
            return "<span class=\"archive-pagination-prev is-disabled\" aria-disabled=\"true\">Previous</span>";
        }

        var previousPath = WebUtility.HtmlEncode(GetArchiveCanonicalPath(currentPage - 1));
        return $"<a class=\"archive-pagination-prev\" rel=\"prev\" href=\"{previousPath}\">Previous</a>";
    }

    private static string RenderArchivePaginationNext(int currentPage, int totalPages)
    {
        if (currentPage >= totalPages)
        {
            return "<span class=\"archive-pagination-next is-disabled\" aria-disabled=\"true\">Next</span>";
        }

        var nextPath = WebUtility.HtmlEncode(GetArchiveCanonicalPath(currentPage + 1));
        return $"<a class=\"archive-pagination-next\" rel=\"next\" href=\"{nextPath}\">Next</a>";
    }

    public static string Slugify(string value) => NewsSlug.Slugify(value);

    public static int ResolveArchiveTotalPages(int currentPage, int itemCount, int publishedCount, int totalPages)
    {
        if (itemCount == ArchivePageSize && totalPages <= currentPage)
        {
            return Math.Max(totalPages, currentPage + 1);
        }

        if (publishedCount > 0)
        {
            return totalPages;
        }

        return itemCount == 0 ? 0 : Math.Max(totalPages, currentPage);
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

    public static string GetNewsDetailPath(NewsItem item) =>
        $"/news/{item.Id}/{NewsSlug.ResolveForArticle(item)}";
}