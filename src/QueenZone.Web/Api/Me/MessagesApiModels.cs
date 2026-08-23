namespace QueenZone.Web;

/// <summary>
/// Inbox row for <c>GET /api/v1/me/messages</c>. Same unread fields as
/// website <c>/messages</c> conversation cards.
/// </summary>
public sealed record InboxConversationDto(
    Guid ConversationId,
    Guid OtherParticipantId,
    string OtherParticipantDisplayName,
    string LastMessagePreview,
    DateTimeOffset LastMessageAt,
    bool HasUnread,
    int UnreadCount,
    string DetailPath);

/// <summary>
/// Header-badge count for <c>GET /api/v1/me/messages/unread-count</c>.
/// Matches <see cref="PrivateMessageService.CountUnreadConversationsAsync"/>.
/// </summary>
public sealed record UnreadConversationsDto(int UnreadConversationCount);

/// <summary>
/// One message in <c>GET /api/v1/me/messages/{conversationId}</c>.
/// Body is the same plain text the website conversation page renders.
/// </summary>
public sealed record ConversationMessageDto(
    Guid Id,
    Guid SenderMemberId,
    string SenderDisplayName,
    string Body,
    DateTimeOffset CreatedAt,
    bool IsMine,
    long SortKey);

/// <summary>
/// Conversation thread for <c>GET /api/v1/me/messages/{conversationId}</c>.
/// Opening this resource marks the conversation read the same way as
/// website <c>GET /messages/{id}</c>. Omit <c>page</c> for the latest page.
/// </summary>
public sealed record ConversationDetailDto(
    Guid ConversationId,
    Guid OtherParticipantId,
    string OtherParticipantDisplayName,
    IReadOnlyList<ConversationMessageDto> Messages,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    string DetailPath);
