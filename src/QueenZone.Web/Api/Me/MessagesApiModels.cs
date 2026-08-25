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
    long SortKey,
    bool ReportedByViewer);

/// <summary>
/// Conversation thread for <c>GET /api/v1/me/messages/{conversationId}</c>
/// and the 201 body of a successful reply POST.
/// Opening GET marks the conversation read the same way as website
/// <c>GET /messages/{id}</c>. Omit <c>page</c> for the latest page.
/// <see cref="CanSendReply"/> matches the website reply composer visibility.
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
    string DetailPath,
    bool CanSendReply);

/// <summary>
/// JSON body for <c>POST /api/v1/me/messages/{conversationId}</c>.
/// Same required/plain-text body as website <c>POST /messages/{id}</c>.
/// </summary>
public sealed record ConversationReplyRequest(string? Body);

/// <summary>
/// One match from <c>GET /api/v1/me/messages/recipients?q=</c>.
/// Same fields as website compose recipient search (<see cref="MemberRecipientMatch"/>).
/// </summary>
public sealed record MessageRecipientDto(Guid MemberId, string DisplayName);

/// <summary>
/// Recipient search results for <c>GET /api/v1/me/messages/recipients?q=</c>.
/// Cap matches website <see cref="PrivateMessageLimits.MaxRecipientSearchResults"/>.
/// </summary>
public sealed record MessageRecipientsDto(IReadOnlyList<MessageRecipientDto> Items);

/// <summary>
/// JSON body for <c>POST /api/v1/me/messages</c> (new conversation / compose).
/// Same recipient + body rules as website <c>POST /messages/compose</c>.
/// </summary>
public sealed record ComposeMessageRequest(Guid? RecipientMemberId, string? Body);

/// <summary>
/// JSON body for <c>POST /api/v1/me/messages/{conversationId}/messages/{messageId}/report</c>.
/// Reason is optional; empty or whitespace is stored as omitted.
/// </summary>
public sealed record ReportMessageRequest(string? Reason);

/// <summary>
/// Result of reporting a private message. <see cref="AlreadyReported"/> is true when
/// this member had already reported the same message (idempotent success).
/// </summary>
public sealed record ReportMessageDto(Guid ReportId, bool AlreadyReported);
