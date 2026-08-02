using QueenZone.Data;

namespace QueenZone.Web;

public sealed class PrivateMessageService(
    IPrivateMessageRepository privateMessageRepository,
    IMemberAccountRepository memberAccountRepository,
    TimeProvider timeProvider)
{
    public Task<IReadOnlyList<PrivateConversationListItem>> GetInboxAsync(
        Guid memberId,
        CancellationToken cancellationToken = default) =>
        privateMessageRepository.GetInboxAsync(memberId, cancellationToken: cancellationToken);

    public Task<int> CountUnreadConversationsAsync(
        Guid memberId,
        CancellationToken cancellationToken = default) =>
        privateMessageRepository.CountUnreadConversationsAsync(memberId, cancellationToken);

    public async Task<PrivateConversationDetail?> GetConversationAsync(
        Guid conversationId,
        Guid memberId,
        bool markRead = true,
        int? page = null,
        int pageSize = PrivateMessageLimits.ConversationPageSize,
        CancellationToken cancellationToken = default)
    {
        var detail = await privateMessageRepository.GetConversationAsync(
            conversationId,
            memberId,
            page,
            pageSize,
            cancellationToken);
        if (detail is null)
        {
            return null;
        }

        // Advance using the commit-ordered SortKey of the last message actually returned on this page.
        // CreatedAt alone is not unique/commit-ordered and can hide delayed or equal-timestamp messages.
        // Opening the default (latest) page marks through the newest message; older pages only advance
        // as far as that page's last message (and cannot move the cursor backwards).
        if (markRead && detail.Messages.Count > 0)
        {
            var lastReturned = detail.Messages[^1];
            await privateMessageRepository.MarkConversationReadAsync(
                conversationId,
                memberId,
                lastReturned.SortKey,
                lastReturned.CreatedAt,
                cancellationToken);
        }

        return detail;
    }

    public async Task<PrivateMessageSendResult> ComposeAsync(
        Guid senderMemberId,
        Guid recipientMemberId,
        string? body,
        CancellationToken cancellationToken = default)
    {
        if (senderMemberId == recipientMemberId)
        {
            return new PrivateMessageSendResult(false, null, "You cannot message yourself.");
        }

        var recipient = await memberAccountRepository.FindByIdAsync(recipientMemberId, cancellationToken);
        if (recipient is null)
        {
            return new PrivateMessageSendResult(false, null, "Recipient was not found.");
        }

        return await privateMessageRepository.SendNewOrExistingAsync(
            senderMemberId,
            recipientMemberId,
            body ?? string.Empty,
            timeProvider.GetUtcNow(),
            cancellationToken);
    }

    public async Task<PrivateMessageSendResult> ReplyAsync(
        Guid conversationId,
        Guid senderMemberId,
        string? body,
        CancellationToken cancellationToken = default)
    {
        var isParticipant = await privateMessageRepository.IsParticipantAsync(
            conversationId,
            senderMemberId,
            cancellationToken);
        if (!isParticipant)
        {
            return new PrivateMessageSendResult(
                false,
                null,
                "You are not a participant in this conversation.");
        }

        return await privateMessageRepository.ReplyAsync(
            conversationId,
            senderMemberId,
            body ?? string.Empty,
            timeProvider.GetUtcNow(),
            cancellationToken);
    }

    public Task<IReadOnlyList<MemberRecipientMatch>> SearchRecipientsAsync(
        Guid currentMemberId,
        string? query,
        CancellationToken cancellationToken = default) =>
        memberAccountRepository.SearchByDisplayNameAsync(
            query ?? string.Empty,
            currentMemberId,
            PrivateMessageLimits.MaxRecipientSearchResults,
            cancellationToken);

    public static bool CanMessage(Guid? currentMemberId, Guid? targetMemberId) =>
        currentMemberId is Guid current
        && targetMemberId is Guid target
        && current != target;
}
