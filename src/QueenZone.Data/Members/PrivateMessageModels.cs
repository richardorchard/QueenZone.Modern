namespace QueenZone.Data;

/// <summary>
/// Shared private-message size caps. Bodies are stored and shown as plain text:
/// no HTML, markdown, or auto-linkification. Renderers must encode, not interpret.
/// </summary>
public static class PrivateMessageLimits
{
    public const int MaxBodyLength = 4000;

    public const int PreviewLength = 160;

    public const int MaxRecipientSearchResults = 20;

    public const int ConversationPageSize = 50;

    public const int MaxConversationPageSize = 100;

    public const int InboxPageSize = 50;

    public const int MaxInboxPageSize = 100;

    public const int MaxReportReasonLength = 1000;

    public const int ReportPrecedingMessageCount = 2;

    /// <summary>
    /// How long a report stays retained after reaching a terminal status (Dismissed/Actioned)
    /// before it is eligible for permanent deletion (ADR 0015 decision 2). Reports in Open or
    /// Reviewed status are never purged by this window.
    /// </summary>
    public static readonly TimeSpan ReportRetentionAfterTerminalStatus = TimeSpan.FromDays(180);
}

public sealed record PrivateConversationListItem(
    Guid ConversationId,
    Guid OtherParticipantId,
    string OtherParticipantDisplayName,
    string LastMessagePreview,
    DateTimeOffset LastMessageAt,
    bool HasUnread,
    int UnreadCount);

public sealed record PrivateInboxPage(
    IReadOnlyList<PrivateConversationListItem> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages =>
        TotalCount <= 0
            ? 1
            : (TotalCount + PageSize - 1) / PageSize;
}

public sealed record PrivateMessageItem(
    Guid Id,
    Guid SenderMemberId,
    string SenderDisplayName,
    string Body,
    DateTimeOffset CreatedAt,
    bool IsMine,
    long SortKey,
    bool ReportedByViewer = false);

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

public sealed record PrivateMessageBlockResult(
    bool Succeeded,
    string? ErrorMessage);

public sealed record MemberFollowResult(
    bool Succeeded,
    string? ErrorMessage);

public sealed record MemberRecipientMatch(
    Guid MemberId,
    string DisplayName);
