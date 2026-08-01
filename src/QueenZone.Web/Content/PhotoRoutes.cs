using QueenZone.Data;

namespace QueenZone.Web;

public static class PhotoRoutes
{
    public const int CategoryPageSize = 24;

    public const string SizeQueryParameter = "size";

    public static string GetCategoriesPath() => "/photography";

    public static string GetCategoryPath(string slug, PhotoListFilter? filter = null) =>
        AppendSizeQuery($"/photography/{slug}", filter);

    public static string GetCategoryPagePath(string slug, int page, PhotoListFilter? filter = null) =>
        AppendSizeQuery(
            page <= 1 ? $"/photography/{slug}" : $"/photography/{slug}/page/{page}",
            filter);

    public static string GetDetailPath(string slug, int picId, PhotoListFilter? filter = null) =>
        AppendSizeQuery($"/photography/{slug}/{picId}", filter);

    public static int GetCategoryTotalPages(int totalCount, int pageSize = CategoryPageSize) =>
        ArchivePagination.GetTotalPages(totalCount, pageSize);

    public static ArchivePaginationViewModel? GetCategoryPaginationViewModel(
        string slug,
        int currentPage,
        int totalPages,
        PhotoListFilter? filter = null) =>
        ArchivePagination.BuildViewModel(
            "Photo collection pagination",
            currentPage,
            totalPages,
            page => GetCategoryPagePath(slug, page, filter));

    public static string AppendSizeQuery(string path, PhotoListFilter? filter)
    {
        if (filter is null || !filter.IsActive || filter.QueryValue is null)
        {
            return path;
        }

        var separator = path.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        return $"{path}{separator}{SizeQueryParameter}={Uri.EscapeDataString(filter.QueryValue)}";
    }
}
