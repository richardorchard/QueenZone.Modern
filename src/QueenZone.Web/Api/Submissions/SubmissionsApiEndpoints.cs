using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using QueenZone.Data;

namespace QueenZone.Web;

/// <summary>
/// Member submission status for the mobile app (issue #745). Reads the same
/// photo, news-suggestion, and article repositories as website
/// <c>/account/my-submissions</c>. Admin approve/reject is visible on the next
/// refresh; there is no extra sync channel.
/// </summary>
public static class SubmissionsApiEndpoints
{
    public const string RootPath = "/api/v1/me/submissions";

    public const string PhotosPath = RootPath + "/photos";

    public const string NewsPath = RootPath + "/news";

    public const string ArticlesPath = RootPath + "/articles";

    public static void MapSubmissionsApiEndpoints(this WebApplication app)
    {
        var group = app.MapGroup(RootPath)
            .WithGroupName(ApiV1.OpenApiDocumentName)
            .WithTags("Submissions")
            .RequireAuthorization(MemberAuthenticationSchemes.MobileMemberPolicy)
            .DisableAntiforgery();

        group.MapGet("/photos", GetPhotosAsync)
            .WithName("GetMyPhotoSubmissions")
            .WithSummary("Paged list of the signed-in member's photo submissions and review status.")
            .Produces<ApiPagedResponse<PhotoSubmissionItemDto>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/news", GetNewsAsync)
            .WithName("GetMyNewsSuggestions")
            .WithSummary("Paged list of the signed-in member's news suggestions and review status.")
            .Produces<ApiPagedResponse<NewsSuggestionItemDto>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/articles", GetArticlesAsync)
            .WithName("GetMyArticleSubmissions")
            .WithSummary("Paged list of the signed-in member's article submissions and review status.")
            .Produces<ApiPagedResponse<ArticleSubmissionItemDto>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    internal static async Task<IResult> GetPhotosAsync(
        HttpContext httpContext,
        [FromServices] IPhotoSubmissionRepository photoSubmissionRepository,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        var unauthorized = UnauthorizedIfMissingMember(httpContext, out var memberId);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        var request = ApiPagination.Normalize(page, pageSize);
        var result = await photoSubmissionRepository.GetBySubmitterAsync(
            memberId, request.Page, request.PageSize, cancellationToken);

        httpContext.Response.Headers.CacheControl = "no-store";
        return Results.Ok(ApiPagedResponse<PhotoSubmissionItemDto>.Create(
            SubmissionsApiMapper.ToPhotos(result.Items),
            request.Page,
            request.PageSize,
            result.TotalCount));
    }

    internal static async Task<IResult> GetNewsAsync(
        HttpContext httpContext,
        [FromServices] INewsSuggestionRepository newsSuggestionRepository,
        [FromServices] INewsRepository newsRepository,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        var unauthorized = UnauthorizedIfMissingMember(httpContext, out var memberId);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        var request = ApiPagination.Normalize(page, pageSize);
        var result = await newsSuggestionRepository.GetBySubmitterAsync(
            memberId, request.Page, request.PageSize, cancellationToken);

        var items = new List<NewsSuggestionItemDto>(result.Items.Count);
        foreach (var suggestion in result.Items)
        {
            var publishedPath = await SubmissionsApiMapper.ResolvePublishedNewsPathAsync(
                suggestion, newsRepository, cancellationToken);
            items.Add(SubmissionsApiMapper.ToNews(suggestion, publishedPath));
        }

        httpContext.Response.Headers.CacheControl = "no-store";
        return Results.Ok(ApiPagedResponse<NewsSuggestionItemDto>.Create(
            items,
            request.Page,
            request.PageSize,
            result.TotalCount));
    }

    internal static async Task<IResult> GetArticlesAsync(
        HttpContext httpContext,
        [FromServices] IArticleSubmissionRepository articleSubmissionRepository,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        var unauthorized = UnauthorizedIfMissingMember(httpContext, out var memberId);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        var request = ApiPagination.Normalize(page, pageSize);
        var result = await articleSubmissionRepository.GetDraftsForMemberAsync(
            memberId, request.Page, request.PageSize, cancellationToken);

        httpContext.Response.Headers.CacheControl = "no-store";
        return Results.Ok(ApiPagedResponse<ArticleSubmissionItemDto>.Create(
            SubmissionsApiMapper.ToArticles(result.Items),
            request.Page,
            request.PageSize,
            result.TotalCount));
    }

    internal static IResult? UnauthorizedIfMissingMember(HttpContext httpContext, out Guid memberId)
    {
        var idValue = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(idValue, out memberId))
        {
            return null;
        }

        memberId = default;
        return Results.Problem(
            statusCode: StatusCodes.Status401Unauthorized,
            title: "Unauthorized",
            detail: "The access token is invalid or expired.");
    }
}
