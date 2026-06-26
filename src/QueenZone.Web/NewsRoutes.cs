using QueenZone.Data;

namespace QueenZone.Web;

public static partial class NewsRoutes
{
    public const int ArchivePageSize = 20;

    public static int GetArchiveTotalPages(int publishedCount, int pageSize = ArchivePageSize) =>
        ArchivePagination.GetTotalPages(publishedCount, pageSize);

    public static string GetArchiveCanonicalPath(int page) =>
        page <= 1 ? "/news" : $"/news/page/{page}";

    public static string GetArchivePageTitle(int page) =>
        page <= 1 ? "QueenZone news" : $"QueenZone news – Page {page}";

    public static ArchivePaginationViewModel? GetArchivePaginationViewModel(int currentPage, int totalPages) =>
        ArchivePagination.BuildViewModel("News archive pagination", currentPage, totalPages, GetArchiveCanonicalPath);

    public static string Slugify(string value) => NewsSlug.Slugify(value);

    public static int ResolveArchiveTotalPages(int currentPage, int itemCount, int publishedCount, int totalPages) =>
        ArchivePagination.ResolveTotalPages(currentPage, itemCount, publishedCount, totalPages, ArchivePageSize);

    public static string GetNewsDetailPath(NewsItem item) =>
        $"/news/{item.Id}/{NewsSlug.ResolveForArticle(item)}";
}