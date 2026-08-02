namespace QueenZone.Data;

public interface IPrivateMessageRepository
{
    Task<IReadOnlyList<PrivateConversationListItem>> GetInboxAsync(
        Guid memberId,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);

    Task<int> CountUnreadConversationsAsync(
        Guid memberId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads one page of messages for a conversation the member participates in.
    /// When <paramref name="page"/> is null or less than 1, returns the latest page
    /// (newest messages). Messages within the page are ordered oldest-first by SortKey.
    /// </summary>
    Task<PrivateConversationDetail?> GetConversationAsync(
        Guid conversationId,
        Guid memberId,
        int? page = null,
        int pageSize = PrivateMessageLimits.ConversationPageSize,
        CancellationToken cancellationToken = default);

    Task<bool> IsParticipantAsync(
        Guid conversationId,
        Guid memberId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Advances the participant read cursor to <paramref name="lastReadSortKey"/> when that value
    /// is newer than the stored cursor. Uses a conditional database update.
    /// </summary>
    Task MarkConversationReadAsync(
        Guid conversationId,
        Guid memberId,
        long lastReadSortKey,
        DateTimeOffset readAt,
        CancellationToken cancellationToken = default);

    Task<PrivateMessageSendResult> SendNewOrExistingAsync(
        Guid senderMemberId,
        Guid recipientMemberId,
        string body,
        DateTimeOffset sentAt,
        CancellationToken cancellationToken = default);

    Task<PrivateMessageSendResult> ReplyAsync(
        Guid conversationId,
        Guid senderMemberId,
        string body,
        DateTimeOffset sentAt,
        CancellationToken cancellationToken = default);
}
