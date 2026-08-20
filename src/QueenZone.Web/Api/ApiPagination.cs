namespace QueenZone.Web;

/// <summary>
/// Shared page/pageSize query rules for <c>/api/v1</c> list endpoints.
/// Invalid values are clamped rather than rejected so mobile clients can retry without a round-trip error.
/// </summary>
public static class ApiPagination
{
    public const int DefaultPage = 1;

    public const int DefaultPageSize = 20;

    public const int MaxPageSize = 100;

    public const string PageQuery = "page";

    public const string PageSizeQuery = "pageSize";

    public static ApiPageRequest Normalize(int? page, int? pageSize)
    {
        var resolvedPage = page is null or < DefaultPage ? DefaultPage : page.Value;
        var resolvedPageSize = pageSize is null or < 1
            ? DefaultPageSize
            : Math.Min(pageSize.Value, MaxPageSize);
        return new ApiPageRequest(resolvedPage, resolvedPageSize);
    }
}

public readonly record struct ApiPageRequest(int Page, int PageSize);
