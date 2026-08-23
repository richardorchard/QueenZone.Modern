using System.Security.Claims;
using QueenZone.Data;

namespace QueenZone.Web;

/// <summary>
/// Signed-in member inbox for the mobile app (issue #737). Reuses
/// <see cref="PrivateMessageService"/> so unread counts match website
/// <c>/messages</c> and the masthead badge. Opening a conversation marks it
/// read the same way as <c>GET /messages/{id}</c>. Requires
/// <see cref="MemberAuthenticationSchemes.MobileMemberPolicy"/>.
/// </summary>
public static class MessagesApiEndpoints
{
    public const string Path = "/api/v1/me/messages";

    public const string UnreadCountPath = $"{Path}/unread-count";

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

        group.MapGet("/messages/{conversationId:guid}", GetConversationAsync)
            .WithName("GetMemberConversation")
            .WithSummary("Open a conversation and mark it read, matching GET /messages/{id}.")
            .Produces<ConversationDetailDto>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
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
        var detail = await privateMessageService.GetConversationAsync(
            conversationId,
            memberId,
            markRead: true,
            page: resolvedPage,
            pageSize: size,
            cancellationToken: cancellationToken);
        if (detail is null)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found",
                detail: "Conversation was not found.");
        }

        httpContext.Response.Headers.CacheControl = "no-store";
        return Results.Ok(MessagesApiMapper.ToConversation(detail));
    }

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
