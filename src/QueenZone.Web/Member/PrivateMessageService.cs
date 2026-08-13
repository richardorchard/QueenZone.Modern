using QueenZone.Data;

namespace QueenZone.Web;

public sealed class PrivateMessageService(
    IPrivateMessageRepository privateMessageRepository,
    IMemberAccountRepository memberAccountRepository,
    TimeProvider timeProvider)
{
    public const string UnableToSendMessage = "Unable to send message.";

    public Task<PrivateInboxPage> GetInboxAsync(
        Guid memberId,
        int page = 1,
        int pageSize = PrivateMessageLimits.InboxPageSize,
        CancellationToken cancellationToken = default) =>
        privateMessageRepository.GetInboxAsync(memberId, page, pageSize, cancellationToken);

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
        if (recipient is null || recipient.DeletionRequestedAt is not null)
        {
            return new PrivateMessageSendResult(false, null, "Recipient was not found.");
        }

        if (await privateMessageRepository.IsMessagingBlockedAsync(
                senderMemberId,
                recipientMemberId,
                cancellationToken))
        {
            return new PrivateMessageSendResult(false, null, UnableToSendMessage);
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

        var otherParticipantId = await privateMessageRepository.GetOtherParticipantIdAsync(
            conversationId,
            senderMemberId,
            cancellationToken);
        if (otherParticipantId is Guid other)
        {
            var otherParticipant = await memberAccountRepository.FindByIdAsync(other, cancellationToken);
            if (otherParticipant?.DeletionRequestedAt is not null
                || await privateMessageRepository.IsMessagingBlockedAsync(
                    senderMemberId,
                    other,
                    cancellationToken))
            {
                return new PrivateMessageSendResult(false, null, UnableToSendMessage);
            }
        }

        return await privateMessageRepository.ReplyAsync(
            conversationId,
            senderMemberId,
            body ?? string.Empty,
            timeProvider.GetUtcNow(),
            cancellationToken);
    }

    public Task<PrivateInboxPage> GetArchivedInboxAsync(
        Guid memberId,
        int page = 1,
        int pageSize = PrivateMessageLimits.InboxPageSize,
        CancellationToken cancellationToken = default) =>
        privateMessageRepository.GetArchivedInboxAsync(memberId, page, pageSize, cancellationToken);

    public Task<bool> ArchiveConversationAsync(
        Guid conversationId,
        Guid memberId,
        CancellationToken cancellationToken = default) =>
        privateMessageRepository.ArchiveConversationAsync(conversationId, memberId, cancellationToken);

    public Task<bool> UnarchiveConversationAsync(
        Guid conversationId,
        Guid memberId,
        CancellationToken cancellationToken = default) =>
        privateMessageRepository.UnarchiveConversationAsync(conversationId, memberId, cancellationToken);

    public Task<bool> RemoveConversationAsync(
        Guid conversationId,
        Guid memberId,
        CancellationToken cancellationToken = default) =>
        privateMessageRepository.RemoveConversationAsync(conversationId, memberId, cancellationToken);

    public async Task<PrivateMessageBlockResult> BlockAsync(
        Guid blockerMemberId,
        Guid blockedMemberId,
        CancellationToken cancellationToken = default)
    {
        if (blockerMemberId == blockedMemberId)
        {
            return new PrivateMessageBlockResult(false, "You cannot block yourself.");
        }

        var target = await memberAccountRepository.FindByIdAsync(blockedMemberId, cancellationToken);
        if (target is null || target.DeletionRequestedAt is not null)
        {
            return new PrivateMessageBlockResult(false, "Member was not found.");
        }

        await privateMessageRepository.BlockAsync(
            blockerMemberId,
            blockedMemberId,
            timeProvider.GetUtcNow(),
            cancellationToken);
        return new PrivateMessageBlockResult(true, null);
    }

    public Task<bool> UnblockAsync(
        Guid blockerMemberId,
        Guid blockedMemberId,
        CancellationToken cancellationToken = default) =>
        privateMessageRepository.UnblockAsync(blockerMemberId, blockedMemberId, cancellationToken);

    public Task<bool> HasBlockedAsync(
        Guid blockerMemberId,
        Guid blockedMemberId,
        CancellationToken cancellationToken = default) =>
        privateMessageRepository.IsBlockedAsync(blockerMemberId, blockedMemberId, cancellationToken);

    public Task<bool> IsMessagingBlockedAsync(
        Guid memberA,
        Guid memberB,
        CancellationToken cancellationToken = default) =>
        privateMessageRepository.IsMessagingBlockedAsync(memberA, memberB, cancellationToken);

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

    public async Task<bool> CanMessageAsync(
        Guid? currentMemberId,
        Guid? targetMemberId,
        CancellationToken cancellationToken = default)
    {
        if (!CanMessage(currentMemberId, targetMemberId))
        {
            return false;
        }

        var target = await memberAccountRepository.FindByIdAsync(targetMemberId!.Value, cancellationToken);
        return target?.DeletionRequestedAt is null
            && !await privateMessageRepository.IsMessagingBlockedAsync(
                currentMemberId!.Value,
                targetMemberId.Value,
                cancellationToken);
    }
}
