using Microsoft.AspNetCore.WebUtilities;

namespace QueenZone.Web;

public static class AdminTimelineRoutes
{
    public static string GetListPath(int page, string? published, string? q)
    {
        var parameters = new Dictionary<string, string?>();

        if (page > 1)
        {
            parameters["pageNumber"] = page.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        if (!string.IsNullOrWhiteSpace(published))
        {
            parameters["published"] = published;
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            parameters["q"] = q;
        }

        return parameters.Count == 0
            ? "/admin/timeline"
            : QueryHelpers.AddQueryString("/admin/timeline", parameters);
    }

    public static ArchivePaginationViewModel? GetListPaginationViewModel(
        int currentPage,
        int totalPages,
        string? published,
        string? q) =>
        ArchivePagination.BuildViewModel(
            "Timeline pagination",
            currentPage,
            totalPages,
            page => GetListPath(page, published, q));
}
