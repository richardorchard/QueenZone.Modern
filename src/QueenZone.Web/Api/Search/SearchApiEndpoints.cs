using QueenZone.Data;
using QueenZone.Web.Pages;

namespace QueenZone.Web;

/// <summary>
/// Public <c>GET /api/v1/search</c> over the shared <c>SearchDocument</c> index
/// (same <see cref="ISiteSearchService"/> visibility as website <c>/search</c>).
/// </summary>
public static class SearchApiEndpoints
{
    public const string Path = "/api/v1/search";

    public static void MapSearchApiEndpoints(this WebApplication app)
    {
        app.MapGet(Path, SearchAsync)
            .WithGroupName(ApiV1.OpenApiDocumentName)
            .WithTags("Search")
            .WithName("GetSiteSearch")
            .WithSummary("Paged whole-site search against the SearchDocument index. Empty q returns an empty page.")
            .DisableAntiforgery()
            .RequireRateLimiting(QueenZoneRateLimitPolicies.Search)
            .Produces<ApiPagedResponse<SearchResultDto>>();
    }

    internal static async Task<IResult> SearchAsync(
        ISiteSearchService siteSearchService,
        string? q,
        string? type,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        var request = ApiPagination.Normalize(page, pageSize, SearchModel.PageSize);
        var contentType = SiteSearchContentType.Normalize(type);
        var results = await siteSearchService.SearchAsync(
            q ?? string.Empty,
            contentType,
            request.Page,
            request.PageSize,
            cancellationToken);

        var response = ApiPagedResponse<SearchResultDto>.Create(
            SearchApiMapper.ToItems(results.Results),
            request.Page,
            request.PageSize,
            results.TotalCount);

        return Results.Ok(response);
    }
}
