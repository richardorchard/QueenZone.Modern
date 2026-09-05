using Microsoft.AspNetCore.WebUtilities;

namespace QueenZone.Web;

public static class AdminTriviaRoutes
{
    public const int ListPageSize = 50;

    public static string GetListPath(int page, string? category)
    {
        var parameters = new Dictionary<string, string?>();

        if (page > 1)
        {
            parameters["pageNumber"] = page.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            parameters["category"] = category;
        }

        return parameters.Count == 0
            ? "/admin/trivia"
            : QueryHelpers.AddQueryString("/admin/trivia", parameters);
    }

    public static ArchivePaginationViewModel? GetListPaginationViewModel(
        int currentPage,
        int totalPages,
        string? category) =>
        ArchivePagination.BuildViewModel(
            "Trivia pagination",
            currentPage,
            totalPages,
            page => GetListPath(page, category));
}
