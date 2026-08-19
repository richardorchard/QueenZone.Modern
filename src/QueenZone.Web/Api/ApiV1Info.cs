namespace QueenZone.Web;

public sealed record ApiV1Info(
    string Version,
    string OpenApi,
    ApiV1Conventions Conventions);

public sealed record ApiV1Conventions(
    ApiV1JsonConvention Json,
    ApiV1ErrorConvention Errors,
    ApiV1PaginationConvention Pagination);

public sealed record ApiV1JsonConvention(
    string PropertyNaming,
    string Dates,
    string Enums);

public sealed record ApiV1ErrorConvention(
    string MediaType,
    string Format,
    string AuthException);

public sealed record ApiV1PaginationConvention(
    string PageQuery,
    string PageSizeQuery,
    int DefaultPage,
    int DefaultPageSize,
    int MaxPageSize,
    IReadOnlyList<string> ResponseFields);
