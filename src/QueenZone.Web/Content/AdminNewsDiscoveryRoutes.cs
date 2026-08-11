using Microsoft.AspNetCore.WebUtilities;
using QueenZone.Data;

namespace QueenZone.Web;

public static class AdminNewsDiscoveryRoutes
{
    public const int ListPageSize = NewsCandidateListQueryDefaults.PageSize;

    public static int GetListTotalPages(int totalCount, int pageSize = ListPageSize) =>
        ArchivePagination.GetTotalPages(totalCount, pageSize);

    public static ArchivePaginationViewModel? GetListPaginationViewModel(
        int currentPage,
        int totalPages,
        Func<int, string> pageHref) =>
        ArchivePagination.BuildViewModel("News discovery review pagination", currentPage, totalPages, pageHref);

    public static string BuildIndexPath(NewsDiscoveryIndexQuery query, int page = 1)
    {
        var parameters = new Dictionary<string, string?>();

        if (page > 1)
        {
            parameters["page"] = page.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        if (query.PageSize != ListPageSize)
        {
            parameters["pageSize"] = query.PageSize.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        if (query.Status is not null)
        {
            parameters["status"] = query.Status.Value.ToString();
        }

        if (query.SourceId is not null)
        {
            parameters["sourceId"] = query.SourceId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        if (query.TrustTier is not null)
        {
            parameters["trustTier"] = query.TrustTier.Value.ToString();
        }

        if (query.MinConfidence is not null)
        {
            parameters["minConfidence"] = query.MinConfidence.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        if (!string.IsNullOrWhiteSpace(query.Entity))
        {
            parameters["entity"] = query.Entity.Trim();
        }

        if (query.DiscoveredFrom is not null)
        {
            parameters["discoveredFrom"] = query.DiscoveredFrom.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        }

        if (query.DiscoveredTo is not null)
        {
            parameters["discoveredTo"] = query.DiscoveredTo.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        }

        if (query.HasDraft is not null)
        {
            parameters["hasDraft"] = query.HasDraft.Value ? "true" : "false";
        }

        return parameters.Count == 0
            ? "/admin/news-discovery"
            : QueryHelpers.AddQueryString("/admin/news-discovery", parameters);
    }
}

public sealed record NewsDiscoveryIndexQuery(
    NewsCandidateStatus? Status = null,
    int? SourceId = null,
    NewsDiscoveryTrustTier? TrustTier = null,
    decimal? MinConfidence = null,
    string? Entity = null,
    DateTime? DiscoveredFrom = null,
    DateTime? DiscoveredTo = null,
    bool? HasDraft = null,
    int PageSize = AdminNewsDiscoveryRoutes.ListPageSize);
