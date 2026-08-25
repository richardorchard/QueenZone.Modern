/** Matches website `/messages` page size (`PrivateMessageLimits.InboxPageSize`). */
export const inboxPageSize = 50;

/** Matches website conversation pages (`PrivateMessageLimits.ConversationPageSize`). */
export const conversationPageSize = 50;

/** Matches `PrivateMessageLimits.MaxBodyLength` / website reply textarea. Bodies are plain text. */
export const conversationBodyMaxLength = 4000;

/** Matches `PrivateMessageLimits.MaxReportReasonLength`. */
export const reportReasonMaxLength = 1000;

export const replyRequiredMessage = 'Message body is required.';

export const replyTooLongMessage = `Message body must be ${conversationBodyMaxLength} characters or fewer.`;

export const reportReasonTooLongMessage = `Report reason must be ${reportReasonMaxLength} characters or fewer.`;

/** Matches `PrivateMessageService.UnableToSendMessage`. */
export const unableToSendMessage = 'Unable to send message.';

/** Matches the blocked-participant notice on `Pages/Messages/Conversation.cshtml`. */
export const youHaveBlockedThisMemberMessage =
  'You have blocked this member. They can no longer send you private messages.';

/**
 * Same priority as `Conversation.cshtml`: the blocked-by-you notice takes
 * precedence over the generic sending-blocked/privacy-disabled notice.
 * Returns null when a reply composer should be shown instead.
 */
export function sendingBlockedNotice(
  hasBlockedOtherParticipant: boolean,
  canSendReply: boolean,
): string | null {
  if (hasBlockedOtherParticipant) {
    return youHaveBlockedThisMemberMessage;
  }
  if (!canSendReply) {
    return unableToSendMessage;
  }
  return null;
}

const guidPattern =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export function parseConversationId(id: string | undefined): string | null {
  const value = id?.trim() ?? '';
  return guidPattern.test(value) ? value : null;
}

export function unreadBadgeLabel(unreadCount: number): string {
  if (unreadCount <= 0) {
    return '';
  }
  return `${unreadCount} unread`;
}

export function messagesA11yLabel(unreadCount: number): string {
  if (unreadCount <= 0) {
    return 'Messages';
  }
  return `Messages, ${unreadCount} unread conversations`;
}

export function profileA11yLabel(unreadCount: number): string {
  if (unreadCount <= 0) {
    return 'Profile';
  }
  return `Profile, ${unreadCount} unread conversations`;
}

export function inboxRowA11yLabel(item: {
  otherParticipantDisplayName: string;
  lastMessagePreview: string;
  unreadCount: number;
}): string {
  const unread = unreadBadgeLabel(item.unreadCount);
  const preview = item.lastMessagePreview.trim();
  return [item.otherParticipantDisplayName, preview, unread].filter((part) => part.length > 0).join('. ');
}

export function formatMessageTimestamp(iso: string): string {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) {
    return '';
  }
  return date.toLocaleString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

export function validateReplyBody(body: string): string | null {
  if (!body.trim()) {
    return replyRequiredMessage;
  }
  if (body.trim().length > conversationBodyMaxLength) {
    return replyTooLongMessage;
  }
  return null;
}

export function validateReportReason(reason: string): string | null {
  if (reason.trim().length > reportReasonMaxLength) {
    return reportReasonTooLongMessage;
  }
  return null;
}
