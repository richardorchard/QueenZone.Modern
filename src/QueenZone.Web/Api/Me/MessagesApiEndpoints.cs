using System.Security.Claims;
using QueenZone.Data;

namespace QueenZone.Web;

/// <summary>
/// Signed-in member inbox, replies, and compose for the mobile app
/// (issues #737 / #738 / #739). Reuses <see cref="PrivateMessageService"/> so
/// unread counts, SortKey assignment, recipient search, and privacy/block
/// rules match website <c>/messages</c>. Opening a conversation marks it
/// read the same way as <c>GET /messages/{id}</c>. Requires
/// <see cref="MemberAuthenticationSchemes.MobileMemberPolicy"/>.
/// </summary>
public static class MessagesApiEndpoints
{
    public const string Path = "/api/v1/me/messages";

    public const string UnreadCountPath = $"{Path}/unread-count";

    public const string RecipientsPath = $"{Path}/recipients";

    public static string ConversationPath(Guid conversationId) => $"{Path}/{conversationId:D}";

    public static void MapMessagesApiEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/me")
            .WithGroupName(ApiV1.OpenApiDocumentName)
            .WithTags("Messages")
            .RequireAuthorization(MemberAuthenticationSchemes.MobileMemberPolicy)
            .DisableAntiforgery();

        group.MapGet("/messages", GetInboxAsync)
            .WithName("GetMemberInbox")
            .WithSummary("Paged inbox conversations with unread counts matching /messages.")
            .Produces<ApiPagedResponse<InboxConversationDto>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/messages/unread-count", GetUnreadCountAsync)
            .WithName("GetMemberUnreadConversationCount")
            .WithSummary("Unread conversation count matching the website messages header badge.")
            .Produces<UnreadConversationsDto>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/messages/recipients", SearchRecipientsAsync)
            .WithName("SearchMemberMessageRecipients")
            .WithSummary("Display-name recipient search matching GET /messages/compose?q=.")
            .Produces<MessageRecipientsDto>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/messages", ComposeAsync)
            .WithName("ComposeMemberMessage")
            .WithSummary("Start or continue a conversation matching POST /messages/compose.")
            .Accepts<ComposeMessageRequest>("application/json")
            .Produces<ConversationDetailDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        group.MapGet("/messages/{conversationId:guid}", GetConversationAsync)
            .WithName("GetMemberConversation")
            .WithSummary("Open a conversation and mark it read, matching GET /messages/{id}.")
            .Produces<ConversationDetailDto>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/messages/{conversationId:guid}", CreateReplyAsync)
            .WithName("ReplyToMemberConversation")
            .WithSummary("Send a reply with the same SortKey rules as POST /messages/{id}.")
            .Accepts<ConversationReplyRequest>("application/json")
            .Produces<ConversationDetailDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
    }

    internal static async Task<IResult> GetInboxAsync(
        HttpContext httpContext,
        ClaimsPrincipal user,
        PrivateMessageService privateMessageService,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        var memberId = RequireMemberId(user, out var unauthorized);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        var request = ApiPagination.Normalize(
            page,
            pageSize,
            PrivateMessageLimits.InboxPageSize,
            PrivateMessageLimits.MaxInboxPageSize);
        var inbox = await privateMessageService.GetInboxAsync(
            memberId,
            request.Page,
            request.PageSize,
            cancellationToken);

        httpContext.Response.Headers.CacheControl = "no-store";
        return Results.Ok(ApiPagedResponse<InboxConversationDto>.Create(
            MessagesApiMapper.ToInboxItems(inbox.Items),
            inbox.Page,
            inbox.PageSize,
            inbox.TotalCount));
    }

    internal static async Task<IResult> GetUnreadCountAsync(
        HttpContext httpContext,
        ClaimsPrincipal user,
        PrivateMessageService privateMessageService,
        CancellationToken cancellationToken)
    {
        var memberId = RequireMemberId(user, out var unauthorized);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        var count = await privateMessageService.CountUnreadConversationsAsync(
            memberId,
            cancellationToken);
        httpContext.Response.Headers.CacheControl = "no-store";
        return Results.Ok(new UnreadConversationsDto(count));
    }

    internal static async Task<IResult> GetConversationAsync(
        HttpContext httpContext,
        ClaimsPrincipal user,
        PrivateMessageService privateMessageService,
        Guid conversationId,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        var memberId = RequireMemberId(user, out var unauthorized);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        var size = ApiPagination.Normalize(
            1,
            pageSize,
            PrivateMessageLimits.ConversationPageSize,
            PrivateMessageLimits.MaxConversationPageSize).PageSize;
        int? resolvedPage = page is null or < 1 ? null : page;
        var mapped = await MapConversationAsync(
            privateMessageService,
            conversationId,
            memberId,
            markRead: true,
            resolvedPage,
            size,
            cancellationToken);
        if (mapped is null)
        {
            return ConversationNotFound();
        }

        httpContext.Response.Headers.CacheControl = "no-store";
        return Results.Ok(mapped);
    }

    internal static async Task<IResult> SearchRecipientsAsync(
        HttpContext httpContext,
        ClaimsPrincipal user,
        PrivateMessageService privateMessageService,
        string? q,
        CancellationToken cancellationToken)
    {
        var memberId = RequireMemberId(user, out var unauthorized);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        var matches = await privateMessageService.SearchRecipientsAsync(
            memberId,
            q,
            cancellationToken);
        httpContext.Response.Headers.CacheControl = "no-store";
        return Results.Ok(MessagesApiMapper.ToRecipients(matches));
    }

    internal static async Task<IResult> ComposeAsync(
        HttpContext httpContext,
        ClaimsPrincipal user,
        PrivateMessageService privateMessageService,
        ComposeMessageRequest? request,
        CancellationToken cancellationToken)
    {
        var memberId = RequireMemberId(user, out var unauthorized);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        if (request is null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request",
                detail: "A JSON body is required.");
        }

        if (request.RecipientMemberId is null || request.RecipientMemberId == Guid.Empty)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request",
                detail: "Choose a recipient.");
        }

        var result = await privateMessageService.ComposeAsync(
            memberId,
            request.RecipientMemberId.Value,
            request.Body,
            cancellationToken);
        if (!result.Succeeded || result.ConversationId is null)
        {
            return MapSendFailure(result);
        }

        var mapped = await MapConversationAsync(
            privateMessageService,
            result.ConversationId.Value,
            memberId,
            markRead: true,
            page: null,
            pageSize: PrivateMessageLimits.ConversationPageSize,
            cancellationToken);
        if (mapped is null)
        {
            return ConversationNotFound();
        }

        httpContext.Response.Headers.CacheControl = "no-store";
        return Results.Created(mapped.DetailPath, mapped);
    }

    internal static async Task<IResult> CreateReplyAsync(
        HttpContext httpContext,
        ClaimsPrincipal user,
        PrivateMessageService privateMessageService,
        Guid conversationId,
        ConversationReplyRequest? request,
        CancellationToken cancellationToken)
    {
        var memberId = RequireMemberId(user, out var unauthorized);
        if (unauthorized is not null)
        {
            return unauthorized;
        }

        if (request is null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request",
                detail: "A JSON body is required.");
        }

        var result = await privateMessageService.ReplyAsync(
            conversationId,
            memberId,
            request.Body,
            cancellationToken);
        if (!result.Succeeded)
        {
            return MapSendFailure(result);
        }

        var mapped = await MapConversationAsync(
            privateMessageService,
            conversationId,
            memberId,
            markRead: true,
            page: null,
            pageSize: PrivateMessageLimits.ConversationPageSize,
            cancellationToken);
        if (mapped is null)
        {
            return ConversationNotFound();
        }

        httpContext.Response.Headers.CacheControl = "no-store";
        return Results.Created(mapped.DetailPath, mapped);
    }

    private static async Task<ConversationDetailDto?> MapConversationAsync(
        PrivateMessageService privateMessageService,
        Guid conversationId,
        Guid memberId,
        bool markRead,
        int? page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var detail = await privateMessageService.GetConversationAsync(
            conversationId,
            memberId,
            markRead,
            page,
            pageSize,
            cancellationToken);
        if (detail is null)
        {
            return null;
        }

        var canSendReply = await privateMessageService.CanSendReplyAsync(
            memberId,
            detail,
            cancellationToken);
        return MessagesApiMapper.ToConversation(detail, canSendReply);
    }

    private static IResult MapSendFailure(PrivateMessageSendResult result)
    {
        var message = result.ErrorMessage ?? PrivateMessageService.UnableToSendMessage;
        if (IsConversationMissing(message))
        {
            return ConversationNotFound();
        }

        if (string.Equals(message, PrivateMessageService.RateLimitedMessage, StringComparison.Ordinal))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status429TooManyRequests,
                title: "Too Many Requests",
                detail: message);
        }

        if (string.Equals(message, PrivateMessageService.UnableToSendMessage, StringComparison.Ordinal))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Forbidden",
                detail: message);
        }

        return Results.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Bad Request",
            detail: message);
    }

    private static bool IsConversationMissing(string message) =>
        string.Equals(message, "You are not a participant in this conversation.", StringComparison.Ordinal)
        || string.Equals(message, "Conversation not found.", StringComparison.Ordinal);

    private static IResult ConversationNotFound() =>
        Results.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Not Found",
            detail: "Conversation was not found.");

    private static Guid RequireMemberId(ClaimsPrincipal user, out IResult? failure)
    {
        var memberId = ForumMember.GetMemberId(user);
        if (memberId is null)
        {
            failure = Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized");
            return Guid.Empty;
        }

        failure = null;
        return memberId.Value;
    }
}
