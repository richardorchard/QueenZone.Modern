using System.Security.Claims;
using System.Text.Json;
using QueenZone.Data;

namespace QueenZone.Web;

/// <summary>
/// <c>/api/v1/forum/*</c> routes for browsing boards, topics, and topic threads
/// (issues #731 / #732) plus authenticated create-topic and reply writes (#733).
/// Reads require no authentication: the website forum index, category, and topic
/// pages are public. Visibility is the same <see cref="IForumRepository"/> path
/// used by Razor Pages — synthetic boards and unvalidated topic starters stay
/// hidden. Writes require <see cref="MemberAuthenticationSchemes.MobileMemberPolicy"/>
/// and reuse <see cref="ForumPostWriteService"/> (sanitization, attachments,
/// <see cref="ForumPostRateLimiter"/>). Attachments reuse the existing
/// member-gated <c>/forum/attachment/...</c> paths. Polls are #734.
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

        group.MapPost("/categories/{id:int}/topics", CreateTopicAsync)
            .WithName("CreateForumTopic")
            .WithSummary("Create a topic in a public board. Same validation and rate limit as /forum/c/{slug}/new-thread.")
            .RequireAuthorization(MemberAuthenticationSchemes.MobileMemberPolicy)
            .Accepts<ForumWriteRequestDto>("application/json")
            .Produces<ForumTopicCreatedDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

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

        group.MapPost("/topics/{id:int}/posts", CreateReplyAsync)
            .WithName("CreateForumReply")
            .WithSummary("Reply to a public topic. Same validation and rate limit as the website topic form.")
            .RequireAuthorization(MemberAuthenticationSchemes.MobileMemberPolicy)
            .Accepts<ForumWriteRequestDto>("application/json")
            .Produces<ForumPostCreatedDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
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

    internal static async Task<IResult> CreateTopicAsync(
        HttpRequest request,
        ClaimsPrincipal user,
        int id,
        ForumPostWriteService writeService,
        CancellationToken cancellationToken)
    {
        var memberId = ForumMember.GetMemberId(user);
        if (memberId is null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized");
        }

        var (title, body, files) = await ReadWriteRequestAsync(request, cancellationToken);
        var outcome = await writeService.CreateTopicAsync(
            memberId.Value,
            user.Identity?.Name,
            id,
            title,
            body,
            files,
            poll: null,
            cancellationToken);

        if (outcome.Succeeded)
        {
            var dto = new ForumTopicCreatedDto(
                outcome.TopicId,
                outcome.PostId,
                outcome.Title,
                ForumRoutes.GetTopicCanonicalPath(outcome.TopicId, outcome.Title));
            return Results.Created($"{RootPath}/topics/{outcome.TopicId}", dto);
        }

        return MapWriteFailure(outcome, categoryId: id, topicId: null);
    }

    internal static async Task<IResult> CreateReplyAsync(
        HttpRequest request,
        ClaimsPrincipal user,
        int id,
        ForumPostWriteService writeService,
        CancellationToken cancellationToken)
    {
        var memberId = ForumMember.GetMemberId(user);
        if (memberId is null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized");
        }

        var (_, body, files) = await ReadWriteRequestAsync(request, cancellationToken);
        var outcome = await writeService.CreateReplyAsync(
            memberId.Value,
            user.Identity?.Name,
            id,
            body,
            files,
            cancellationToken);

        if (outcome.Succeeded)
        {
            var dto = new ForumPostCreatedDto(
                outcome.PostId,
                outcome.TopicId,
                ForumRoutes.GetTopicCanonicalPath(outcome.TopicId, outcome.Title) + $"#post-{outcome.PostId}");
            return Results.Created($"{RootPath}/topics/{outcome.TopicId}/posts", dto);
        }

        return MapWriteFailure(outcome, categoryId: null, topicId: id);
    }

    private static IResult MapWriteFailure(ForumWriteOutcome outcome, int? categoryId, int? topicId)
    {
        return outcome.Status switch
        {
            ForumWriteStatus.CategoryNotFound => Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found",
                detail: $"No public forum board with id '{categoryId}'."),
            ForumWriteStatus.TopicNotFound => TopicNotFound(topicId ?? 0),
            ForumWriteStatus.TopicLocked => Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Forbidden",
                detail: ForumPostWriteService.TopicLockedMessage),
            ForumWriteStatus.MemberSuspended => Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Forbidden",
                detail: ForumPostWriteService.SuspendedMessage),
            ForumWriteStatus.RateLimited => Results.Problem(
                statusCode: StatusCodes.Status429TooManyRequests,
                title: "Too Many Requests",
                detail: ForumPostWriteService.RateLimitedMessage),
            ForumWriteStatus.ValidationFailed or ForumWriteStatus.AttachmentFailed => Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request",
                detail: string.Join(' ', outcome.FieldErrors.Select(error => error.Message))),
            _ => Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Server Error",
                detail: "Unable to save this post."),
        };
    }

    private static async Task<(string? Title, string? Body, IReadOnlyList<IFormFile> Files)> ReadWriteRequestAsync(
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (request.HasFormContentType)
        {
            var form = await request.ReadFormAsync(cancellationToken);
            var title = FirstNonEmpty(
                form["title"].ToString(),
                form["Title"].ToString(),
                form["subject"].ToString(),
                form["Subject"].ToString());
            var body = FirstNonEmpty(form["body"].ToString(), form["Body"].ToString());
            var files = form.Files.Where(file => file is { Length: > 0 }).ToList();
            return (title, body, files);
        }

        try
        {
            var json = await request.ReadFromJsonAsync<ForumWriteRequestDto>(cancellationToken);
            return (json?.ResolvedTitle, json?.Body, []);
        }
        catch (JsonException)
        {
            return (null, null, []);
        }
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static IResult TopicNotFound(int id) =>
        Results.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Not Found",
            detail: $"No public forum topic with id '{id}'.");
}
