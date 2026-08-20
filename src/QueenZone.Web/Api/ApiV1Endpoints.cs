namespace QueenZone.Web;

public static class ApiV1Endpoints
{
    public static void MapQueenZoneApiV1(this WebApplication app)
    {
        app.MapGet(ApiV1.Prefix, GetInfo)
            .WithGroupName(ApiV1.OpenApiDocumentName)
            .WithName("GetApiV1Info")
            .WithTags("Meta")
            .DisableAntiforgery()
            .Produces<ApiV1Info>();

        app.MapOpenApi(ApiV1.OpenApiRoutePattern)
            .AllowAnonymous()
            .WithName("GetApiV1OpenApiDocument");

        app.MapFallback("/api/v1/{*path}", NotFound)
            .ExcludeFromDescription()
            .DisableAntiforgery();
    }

    internal static ApiV1Info GetInfo() => new(
        Version: ApiV1.Version,
        OpenApi: ApiV1.OpenApiPath,
        Conventions: new ApiV1Conventions(
            Json: new ApiV1JsonConvention(
                PropertyNaming: "camelCase",
                Dates: "ISO-8601 UTC",
                Enums: "string"),
            Errors: new ApiV1ErrorConvention(
                MediaType: "application/problem+json",
                Format: "RFC 7807 Problem Details",
                AuthException: "RFC 6749 { error, error_description } on /api/v1/auth token and authorize responses"),
            Pagination: new ApiV1PaginationConvention(
                PageQuery: ApiPagination.PageQuery,
                PageSizeQuery: ApiPagination.PageSizeQuery,
                DefaultPage: ApiPagination.DefaultPage,
                DefaultPageSize: ApiPagination.DefaultPageSize,
                MaxPageSize: ApiPagination.MaxPageSize,
                ResponseFields: ["items", "page", "pageSize", "totalCount", "totalPages"])));

    internal static IResult NotFound(HttpContext context) =>
        Results.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Not Found",
            detail: $"No API resource matches '{context.Request.Path}'.");
}
