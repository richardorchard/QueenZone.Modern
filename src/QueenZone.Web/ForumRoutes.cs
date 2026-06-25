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

    public static string BuildCategoryPaginationNav(ForumCategoryItem category, int currentPage, int totalPages) =>
        ArchivePagination.BuildNav(
            "Forum topics pagination",
            currentPage,
            totalPages,
            page => GetCategoryCanonicalPath(category, page));

    public static string BuildTopicPaginationNav(ForumTopicHeader header, int currentPage, int totalPages) =>
        ArchivePagination.BuildNav(
            "Forum posts pagination",
            currentPage,
            totalPages,
            page => GetTopicCanonicalPath(header, page));
}