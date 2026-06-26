using QueenZone.Data;

namespace QueenZone.Web;

public static class PhotoRoutes
{
    public const int CategoryPageSize = 12;

    public static string GetCategoriesPath() => "/photography";

    public static string GetCategoryPath(string slug) => $"/photography/{slug}";

    public static string GetCategoryPagePath(string slug, int page) =>
        page <= 1 ? GetCategoryPath(slug) : $"/photography/{slug}/page/{page}";

    public static string GetDetailPath(string slug, int picId) => $"/photography/{slug}/{picId}";

    public static int GetCategoryTotalPages(int totalCount, int pageSize = CategoryPageSize) =>
        ArchivePagination.GetTotalPages(totalCount, pageSize);

    public static ArchivePaginationViewModel? GetCategoryPaginationViewModel(string slug, int currentPage, int totalPages) =>
        ArchivePagination.BuildViewModel(
            "Photo collection pagination",
            currentPage,
            totalPages,
            page => GetCategoryPagePath(slug, page));
}
