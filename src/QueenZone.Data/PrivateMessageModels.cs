namespace QueenZone.Data;

public static class PrivateMessageLimits
{
    public const int MaxBodyLength = 4000;

    public const int PreviewLength = 160;

    public const int MaxRecipientSearchResults = 20;

    public const int ConversationPageSize = 50;

    public const int MaxConversationPageSize = 100;
}

public sealed record PrivateConversationListItem(
    Guid ConversationId,
    Guid OtherParticipantId,
    string OtherParticipantDisplayName,
    string LastMessagePreview,
    DateTimeOffset LastMessageAt,
    bool HasUnread,
    int UnreadCount);

public sealed record PrivateMessageItem(
    Guid Id,
    Guid SenderMemberId,
    string SenderDisplayName,
    string Body,
    DateTimeOffset CreatedAt,
    bool IsMine,
    long SortKey);

public sealed record PrivateConversationDetail(
    Guid ConversationId,
    Guid OtherParticipantId,
    string OtherParticipantDisplayName,
    IReadOnlyList<PrivateMessageItem> Messages,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages =>
        TotalCount <= 0
            ? 1
            : (TotalCount + PageSize - 1) / PageSize;
}

public sealed record PrivateMessageSendResult(
    bool Succeeded,
    Guid? ConversationId,
    string? ErrorMessage);

public sealed record MemberRecipientMatch(
    Guid MemberId,
    string DisplayName);
