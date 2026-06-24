using System.Net;
using System.Text;
using QueenZone.Data;

namespace QueenZone.Web;

public static class ForumRoutes
{
    public const int TopicsPageSize = 25;

    public const int PostsPageSize = 15;

    public static string GetCategoryPath(ForumCategoryItem category) =>
        GetCategoryCanonicalPath(category);

    public static string GetCategoryCanonicalPath(ForumCategoryItem category, int page = 1) =>
        GetCategoryCanonicalPath(category.Id, category.Name, page);

    public static string GetCategoryCanonicalPath(int id, string name, int page = 1)
    {
        var slug = NewsSlug.Slugify(name);
        return page <= 1 ? $"/forum/{id}/{slug}" : $"/forum/{id}/{slug}/page/{page}";
    }

    public static string GetCategoryPageTitle(ForumCategoryItem category, int page) =>
        page <= 1 ? $"{category.Name} | QueenZone forum" : $"{category.Name} – Page {page} | QueenZone forum";

    public static string GetTopicPath(ForumTopicItem topic) =>
        GetTopicCanonicalPath(topic.Id, topic.Title);

    public static string GetTopicCanonicalPath(ForumTopicHeader header, int page = 1) =>
        GetTopicCanonicalPath(header.TopicId, header.Title, page);

    public static string GetTopicCanonicalPath(int topicId, string title, int page = 1)
    {
        var slug = NewsSlug.Slugify(title);
        return page <= 1 ? $"/forum/topic/{topicId}/{slug}" : $"/forum/topic/{topicId}/{slug}/page/{page}";
    }

    public static string GetTopicPageTitle(ForumTopicHeader header, int page) =>
        page <= 1 ? $"{header.Title.Trim()} | {header.ForumName.Trim()} | QueenZone forum"
            : $"{header.Title.Trim()} – Page {page} | QueenZone forum";

    public static int GetPostsTotalPages(int totalCount, int pageSize = PostsPageSize) =>
        GetTopicsTotalPages(totalCount, pageSize);

    public static int GetTopicsTotalPages(int totalCount, int pageSize = TopicsPageSize)
    {
        if (totalCount <= 0)
        {
            return 0;
        }

        return (totalCount + pageSize - 1) / pageSize;
    }

    public static string FormatCount(long value) =>
        value >= 1_000_000
            ? $"{value / 1_000_000.0:0.#}M+"
            : value >= 1_000
                ? $"{value / 1_000.0:0.#}k+"
                : value.ToString("N0");

    public static string BuildCategoryPaginationNav(ForumCategoryItem category, int currentPage, int totalPages)
    {
        if (totalPages <= 1)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        builder.Append("<nav class=\"archive-pagination\" aria-label=\"Forum topics pagination\">");
        builder.Append("<p class=\"archive-pagination-summary\">Page ");
        builder.Append(currentPage);
        builder.Append(" of ");
        builder.Append(totalPages);
        builder.Append("</p>");
        builder.Append("<div class=\"archive-pagination-controls\">");
        builder.Append(RenderCategoryPaginationPrevious(category, currentPage));
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
                builder.Append(WebUtility.HtmlEncode(GetCategoryCanonicalPath(category, pageNumber.Value)));
                builder.Append("\">");
                builder.Append(pageNumber);
                builder.Append("</a>");
            }

            builder.Append("</li>");
        }

        builder.Append("</ol>");
        builder.Append(RenderCategoryPaginationNext(category, currentPage, totalPages));
        builder.Append("</div></nav>");
        return builder.ToString();
    }

    public static string BuildTopicPaginationNav(ForumTopicHeader header, int currentPage, int totalPages)
    {
        if (totalPages <= 1)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        builder.Append("<nav class=\"archive-pagination\" aria-label=\"Forum posts pagination\">");
        builder.Append("<p class=\"archive-pagination-summary\">Page ");
        builder.Append(currentPage);
        builder.Append(" of ");
        builder.Append(totalPages);
        builder.Append("</p>");
        builder.Append("<div class=\"archive-pagination-controls\">");
        builder.Append(RenderTopicPaginationPrevious(header, currentPage));
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
                builder.Append(WebUtility.HtmlEncode(GetTopicCanonicalPath(header, pageNumber.Value)));
                builder.Append("\">");
                builder.Append(pageNumber);
                builder.Append("</a>");
            }

            builder.Append("</li>");
        }

        builder.Append("</ol>");
        builder.Append(RenderTopicPaginationNext(header, currentPage, totalPages));
        builder.Append("</div></nav>");
        return builder.ToString();
    }

    private static string RenderTopicPaginationPrevious(ForumTopicHeader header, int currentPage)
    {
        if (currentPage <= 1)
        {
            return "<span class=\"archive-pagination-prev is-disabled\" aria-disabled=\"true\">Previous</span>";
        }

        var previousPath = WebUtility.HtmlEncode(GetTopicCanonicalPath(header, currentPage - 1));
        return $"<a class=\"archive-pagination-prev\" rel=\"prev\" href=\"{previousPath}\">Previous</a>";
    }

    private static string RenderTopicPaginationNext(ForumTopicHeader header, int currentPage, int totalPages)
    {
        if (currentPage >= totalPages)
        {
            return "<span class=\"archive-pagination-next is-disabled\" aria-disabled=\"true\">Next</span>";
        }

        var nextPath = WebUtility.HtmlEncode(GetTopicCanonicalPath(header, currentPage + 1));
        return $"<a class=\"archive-pagination-next\" rel=\"next\" href=\"{nextPath}\">Next</a>";
    }

    private static string RenderCategoryPaginationPrevious(ForumCategoryItem category, int currentPage)
    {
        if (currentPage <= 1)
        {
            return "<span class=\"archive-pagination-prev is-disabled\" aria-disabled=\"true\">Previous</span>";
        }

        var previousPath = WebUtility.HtmlEncode(GetCategoryCanonicalPath(category, currentPage - 1));
        return $"<a class=\"archive-pagination-prev\" rel=\"prev\" href=\"{previousPath}\">Previous</a>";
    }

    private static string RenderCategoryPaginationNext(ForumCategoryItem category, int currentPage, int totalPages)
    {
        if (currentPage >= totalPages)
        {
            return "<span class=\"archive-pagination-next is-disabled\" aria-disabled=\"true\">Next</span>";
        }

        var nextPath = WebUtility.HtmlEncode(GetCategoryCanonicalPath(category, currentPage + 1));
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