using QueenZone.Data;

namespace QueenZone.Web;

/// <summary>
/// Public, read-only <c>/api/v1/content/*</c> routes for the mobile app (issue #726).
/// No authentication required: this content is public on the website today.
/// </summary>
public static class ContentApiEndpoints
{
    public const string RootPath = "/api/v1/content";

    public static void MapContentApiEndpoints(this WebApplication app)
    {
        var group = app.MapGroup(RootPath)
            .WithGroupName(ApiV1.OpenApiDocumentName)
            .WithTags("Content")
            .DisableAntiforgery();

        group.MapGet("/biography", GetBiographyChaptersAsync)
            .WithName("GetContentBiographyChapters")
            .WithSummary("Paged list of biography chapters, in reading order.")
            .Produces<ApiPagedResponse<BiographyChapterListItemDto>>();

        group.MapGet("/biography/{id:int}", GetBiographyChapterDetailAsync)
            .WithName("GetContentBiographyChapterDetail")
            .WithSummary("A single biography chapter, with adjacent-chapter navigation.")
            .Produces<BiographyChapterDetailDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    internal static async Task<IResult> GetBiographyChaptersAsync(
        IBiographyRepository biographyRepository,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        var request = ApiPagination.Normalize(page, pageSize);
        var chapters = BiographyChapterOrdering.ByDisplaySequenceAscending(
            await biographyRepository.GetChaptersAsync(cancellationToken));

        var pageItems = chapters
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var response = ApiPagedResponse<BiographyChapterListItemDto>.Create(
            ContentApiMapper.ToBiographyChapterListItems(pageItems),
            request.Page,
            request.PageSize,
            chapters.Count);

        return Results.Ok(response);
    }

    internal static async Task<IResult> GetBiographyChapterDetailAsync(
        IBiographyRepository biographyRepository,
        int id,
        CancellationToken cancellationToken)
    {
        var chapter = await biographyRepository.GetByIdAsync(id, cancellationToken);
        if (chapter is null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found",
                detail: $"No biography chapter with id '{id}'.");
        }

        var navigation = await biographyRepository.GetAdjacentChaptersAsync(id, cancellationToken);
        return Results.Ok(ContentApiMapper.ToBiographyChapterDetail(chapter, navigation));
    }
}
