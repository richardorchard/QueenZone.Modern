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

    Task<PrivateConversationDetail?> GetConversationAsync(
        Guid conversationId,
        Guid memberId,
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
