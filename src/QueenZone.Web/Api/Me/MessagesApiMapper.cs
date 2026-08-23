using QueenZone.Data;

namespace QueenZone.Web;

/// <summary>
/// Maps private-message service models to <c>/api/v1/me/messages</c> JSON.
/// <see cref="InboxConversationDto.DetailPath"/> reuses the website conversation URL.
/// </summary>
public static class MessagesApiMapper
{
    public static string ConversationDetailPath(Guid conversationId) =>
        $"/messages/{conversationId:D}";

    public static InboxConversationDto ToInboxItem(PrivateConversationListItem item) =>
        new(
            item.ConversationId,
            item.OtherParticipantId,
            item.OtherParticipantDisplayName,
            item.LastMessagePreview,
            item.LastMessageAt,
            item.HasUnread,
            item.UnreadCount,
            ConversationDetailPath(item.ConversationId));

    public static IReadOnlyList<InboxConversationDto> ToInboxItems(
        IEnumerable<PrivateConversationListItem> items) =>
        items.Select(ToInboxItem).ToList();

    public static ConversationMessageDto ToMessage(PrivateMessageItem message) =>
        new(
            message.Id,
            message.SenderMemberId,
            message.SenderDisplayName,
            message.Body,
            message.CreatedAt,
            message.IsMine,
            message.SortKey);

    public static ConversationDetailDto ToConversation(
        PrivateConversationDetail detail,
        bool canSendReply) =>
        new(
            detail.ConversationId,
            detail.OtherParticipantId,
            detail.OtherParticipantDisplayName,
            detail.Messages.Select(ToMessage).ToList(),
            detail.Page,
            detail.PageSize,
            detail.TotalCount,
            detail.TotalPages,
            ConversationDetailPath(detail.ConversationId),
            canSendReply);

    public static MessageRecipientDto ToRecipient(MemberRecipientMatch match) =>
        new(match.MemberId, match.DisplayName);

    public static MessageRecipientsDto ToRecipients(IEnumerable<MemberRecipientMatch> matches) =>
        new(matches.Select(ToRecipient).ToList());
}
