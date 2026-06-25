using QueenZone.Data;

namespace QueenZone.Web;

public static partial class ArticlesRoutes
{
    public const int ArchivePageSize = 20;

    public static int GetArchiveTotalPages(int publishedCount, int pageSize = ArchivePageSize) =>
        ArchivePagination.GetTotalPages(publishedCount, pageSize);

    public static string GetArchiveCanonicalPath(int page) =>
        page <= 1 ? "/articles" : $"/articles/page/{page}";

    public static string GetArchivePageTitle(int page) =>
        page <= 1 ? "QueenZone articles" : $"QueenZone articles – Page {page}";

    public static string BuildArchivePaginationNav(int currentPage, int totalPages) =>
        ArchivePagination.BuildNav("Articles archive pagination", currentPage, totalPages, GetArchiveCanonicalPath);

    public static int ResolveArchiveTotalPages(int currentPage, int itemCount, int publishedCount, int totalPages) =>
        ArchivePagination.ResolveTotalPages(currentPage, itemCount, publishedCount, totalPages, ArchivePageSize);

    public static string GetArticleDetailPath(ArticleItem item) =>
        GetArticleDetailPath(item.Id, item.Title);

    public static string GetArticleDetailPath(int id, string title) =>
        $"/articles/{id}/{NewsSlug.Slugify(title)}";
}