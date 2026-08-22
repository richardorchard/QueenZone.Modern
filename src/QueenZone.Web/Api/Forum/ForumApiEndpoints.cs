using QueenZone.Data;

namespace QueenZone.Web;

/// <summary>
/// Public, read-only <c>/api/v1/forum/*</c> routes for browsing boards, topics,
/// and topic threads (issues #731 / #732). No authentication required: the
/// website forum index, category, and topic pages are public. Visibility is the
/// same <see cref="IForumRepository"/> path used by Razor Pages — synthetic
/// boards and unvalidated topic starters stay hidden. Attachments reuse the
/// existing member-gated <c>/forum/attachment/...</c> paths.
/// </summary>
public static class ForumApiEndpoints
{
    public const string RootPath = "/api/v1/forum";

    public static void MapForumApiEndpoints(this WebApplication app)
    {
        var group = app.MapGroup(RootPath)
            .WithGroupName(ApiV1.OpenApiDocumentName)
            .WithTags("Forum")
            .DisableAntiforgery();

        group.MapGet("/categories", GetCategoriesAsync)
            .WithName("GetForumCategories")
            .WithSummary("Paged list of public forum boards, in website sort order.")
            .Produces<ApiPagedResponse<ForumCategoryListItemDto>>();

        group.MapGet("/categories/{id:int}", GetCategoryAsync)
            .WithName("GetForumCategory")
            .WithSummary("A single public forum board.")
            .Produces<ForumCategoryListItemDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/categories/{id:int}/topics", GetCategoryTopicsAsync)
            .WithName("GetForumCategoryTopics")
            .WithSummary("Paged public topics in a board. Sticky threads first, then last activity.")
            .Produces<ApiPagedResponse<ForumTopicListItemDto>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/topics/{id:int}", GetTopicAsync)
            .WithName("GetForumTopic")
            .WithSummary("A single public forum topic header.")
            .Produces<ForumTopicDetailDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/topics/{id:int}/posts", GetTopicPostsAsync)
            .WithName("GetForumTopicPosts")
            .WithSummary("Paged public posts in a topic, chronological, matching website pages.")
            .Produces<ApiPagedResponse<ForumPostDto>>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    internal static async Task<IResult> GetCategoriesAsync(
        IForumRepository forumRepository,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        var request = ApiPagination.Normalize(page, pageSize);
        var categories = await forumRepository.GetCategoriesAsync(cancellationToken);

        var pageItems = categories
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var response = ApiPagedResponse<ForumCategoryListItemDto>.Create(
            ForumApiMapper.ToCategoryListItems(pageItems),
            request.Page,
            request.PageSize,
            categories.Count);

        return Results.Ok(response);
    }

    internal static async Task<IResult> GetCategoryAsync(
        IForumRepository forumRepository,
        int id,
        CancellationToken cancellationToken)
    {
        var category = await forumRepository.GetCategoryByIdAsync(id, cancellationToken);
        if (category is null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found",
                detail: $"No public forum board with id '{id}'.");
        }

        return Results.Ok(ForumApiMapper.ToCategoryListItem(category));
    }

    internal static async Task<IResult> GetCategoryTopicsAsync(
        IForumRepository forumRepository,
        int id,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        var category = await forumRepository.GetCategoryByIdAsync(id, cancellationToken);
        if (category is null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found",
                detail: $"No public forum board with id '{id}'.");
        }

        var request = ApiPagination.Normalize(page, pageSize);
        var topicsPage = await forumRepository.GetCategoryTopicsPageAsync(
            id,
            request.Page,
            request.PageSize,
            cancellationToken);

        var response = ApiPagedResponse<ForumTopicListItemDto>.Create(
            ForumApiMapper.ToTopicListItems(topicsPage.Topics),
            request.Page,
            request.PageSize,
            topicsPage.TotalCount);

        return Results.Ok(response);
    }

    internal static async Task<IResult> GetTopicAsync(
        IForumRepository forumRepository,
        int id,
        CancellationToken cancellationToken)
    {
        var topicPage = await forumRepository.GetTopicPostsPageAsync(id, 1, 1, cancellationToken);
        if (topicPage is null)
        {
            return TopicNotFound(id);
        }

        return Results.Ok(ForumApiMapper.ToTopicDetail(topicPage.Header, topicPage.TotalCount));
    }

    internal static async Task<IResult> GetTopicPostsAsync(
        IForumRepository forumRepository,
        UgcHtml ugcHtml,
        int id,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        var request = ApiPagination.Normalize(page, pageSize);
        var topicPage = await forumRepository.GetTopicPostsPageAsync(
            id,
            request.Page,
            request.PageSize,
            cancellationToken);
        if (topicPage is null)
        {
            return TopicNotFound(id);
        }

        var response = ApiPagedResponse<ForumPostDto>.Create(
            ForumApiMapper.ToPosts(topicPage.Posts, ugcHtml),
            request.Page,
            request.PageSize,
            topicPage.TotalCount);

        return Results.Ok(response);
    }

    private static IResult TopicNotFound(int id) =>
        Results.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Not Found",
            detail: $"No public forum topic with id '{id}'.");
}
