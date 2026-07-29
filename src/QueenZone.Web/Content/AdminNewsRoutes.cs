namespace QueenZone.Web;

public static class AdminNewsRoutes
{
    public const string AntiforgeryTokenFieldName = "__RequestVerificationToken";

    public const int ListPageSize = 50;

    public static int GetListTotalPages(int totalCount, int pageSize = ListPageSize) =>
        ArchivePagination.GetTotalPages(totalCount, pageSize);

    public static string GetListPath(int page) =>
        page <= 1 ? "/admin/news" : $"/admin/news/page/{page}";

    public static ArchivePaginationViewModel? GetListPaginationViewModel(int currentPage, int totalPages) =>
        ArchivePagination.BuildViewModel("Admin news pagination", currentPage, totalPages, GetListPath);
}
