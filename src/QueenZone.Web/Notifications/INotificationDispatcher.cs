namespace QueenZone.Web;

/// <summary>
/// Category-specific push dispatch after a successful write. Implementations
/// must swallow and log failures — never rethrow to the write path.
/// </summary>
public interface INotificationDispatcher
{
    Task NotifyForumReplyAsync(
        int topicId,
        int postId,
        Guid authorMemberId,
        string topicTitle,
        CancellationToken cancellationToken = default);

    Task NotifyPrivateMessageAsync(
        Guid conversationId,
        Guid recipientMemberId,
        Guid senderMemberId,
        CancellationToken cancellationToken = default);

    Task NotifyNewsPublishedAsync(
        int articleId,
        string title,
        CancellationToken cancellationToken = default);
}
