namespace QueenZone.Web;

public static class FanPerformanceRoutes
{
    public const int PageSize = 20;

    public static string GetIndexPath() => "/fan-performances";

    public static string GetPagePath(int page) =>
        page <= 1 ? GetIndexPath() : $"/fan-performances/page/{page}";

    public static string GetAudioPath(int id) => $"/fan-performances/{id}/audio";

    public static string GetLoginPath(string returnPath) =>
        $"/account/login?returnUrl={Uri.EscapeDataString(returnPath)}";

    public static int GetTotalPages(int visibleCount, int pageSize = PageSize) =>
        ArchivePagination.GetTotalPages(visibleCount, pageSize);

    public static int ResolveTotalPages(int currentPage, int itemCount, int visibleCount, int totalPages) =>
        ArchivePagination.ResolveTotalPages(currentPage, itemCount, visibleCount, totalPages, PageSize);

    public static ArchivePaginationViewModel? GetPaginationViewModel(int currentPage, int totalPages) =>
        ArchivePagination.BuildViewModel("Fan performances pagination", currentPage, totalPages, GetPagePath);
}
