namespace QueenZone.Web;

public static class FreddieTributeRoutes
{
    public const int PageSize = 12;

    public static string GetIndexPath() => "/freddie-mercury-tribute";

    public static string GetPagePath(int page) =>
        page <= 1 ? GetIndexPath() : $"/freddie-mercury-tribute/page/{page}";

    public static int GetTotalPages(int visibleCount, int pageSize = PageSize) =>
        ArchivePagination.GetTotalPages(visibleCount, pageSize);

    public static int ResolveTotalPages(int currentPage, int itemCount, int visibleCount, int totalPages) =>
        ArchivePagination.ResolveTotalPages(currentPage, itemCount, visibleCount, totalPages, PageSize);

    public static ArchivePaginationViewModel? GetPaginationViewModel(int currentPage, int totalPages) =>
        ArchivePagination.BuildViewModel("Freddie Mercury tribute pagination", currentPage, totalPages, GetPagePath);
}

