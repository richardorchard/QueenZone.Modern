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

        group.MapGet("/discography", GetAlbumsAsync)
            .WithName("GetContentDiscographyAlbums")
            .WithSummary("Paged list of studio albums.")
            .Produces<ApiPagedResponse<AlbumListItemDto>>();

        group.MapGet("/discography/{id:int}", GetAlbumDetailAsync)
            .WithName("GetContentDiscographyAlbumDetail")
            .WithSummary("A single studio album, with its track list.")
            .Produces<AlbumDetailDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    internal static async Task<IResult> GetAlbumsAsync(
        IDiscographyRepository discographyRepository,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        var request = ApiPagination.Normalize(page, pageSize);
        var albums = await discographyRepository.GetAlbumsAsync(cancellationToken);

        var pageItems = albums
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var response = ApiPagedResponse<AlbumListItemDto>.Create(
            ContentApiMapper.ToAlbumListItems(pageItems),
            request.Page,
            request.PageSize,
            albums.Count);

        return Results.Ok(response);
    }

    internal static async Task<IResult> GetAlbumDetailAsync(
        IDiscographyRepository discographyRepository,
        int id,
        CancellationToken cancellationToken)
    {
        var album = await discographyRepository.GetAlbumByIdAsync(id, cancellationToken);
        if (album is null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found",
                detail: $"No album with id '{id}'.");
        }

        return Results.Ok(ContentApiMapper.ToAlbumDetail(album));
    }
}
