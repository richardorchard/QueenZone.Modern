namespace QueenZone.Web;

/// <summary>
/// Standard list envelope for <c>/api/v1</c> collection endpoints.
/// </summary>
public sealed record ApiPagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages)
{
    public static ApiPagedResponse<T> Create(
        IReadOnlyList<T> items,
        int page,
        int pageSize,
        int totalCount)
    {
        ArgumentNullException.ThrowIfNull(items);
        var request = ApiPagination.Normalize(page, pageSize);
        var safeTotalCount = Math.Max(totalCount, 0);
        var totalPages = safeTotalCount == 0
            ? 0
            : (int)Math.Ceiling(safeTotalCount / (double)request.PageSize);
        return new ApiPagedResponse<T>(items, request.Page, request.PageSize, safeTotalCount, totalPages);
    }
}
