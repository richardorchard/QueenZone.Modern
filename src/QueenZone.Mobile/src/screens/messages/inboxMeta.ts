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

/**
 * Monogram initials for the thread header/avatar (design handoff
 * `design/design_handoff_private_messages`). First letter of the first and
 * last words — e.g. "Richard Orchard TW" → "RT" — falling back to the first
 * two characters of a single-word name.
 */
export function initialsFor(displayName: string): string {
  const parts = displayName.trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) {
    return '';
  }
  if (parts.length === 1) {
    return parts[0].slice(0, 2).toUpperCase();
  }
  return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
}

/** 24-hour clock time only, e.g. "17:10" — used for per-message attribution lines. */
export function formatMessageClockTime(iso: string): string {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) {
    return '';
  }
  const hours = String(date.getHours()).padStart(2, '0');
  const minutes = String(date.getMinutes()).padStart(2, '0');
  return `${hours}:${minutes}`;
}

/**
 * Date-divider label: "TODAY" / "YESTERDAY" for the last two days, otherwise
 * British long-date style, e.g. "2 AUGUST 2026".
 */
export function formatDateDividerLabel(iso: string, now: Date = new Date()): string {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) {
    return '';
  }
  const startOfDay = (value: Date) => new Date(value.getFullYear(), value.getMonth(), value.getDate()).getTime();
  const diffDays = Math.round((startOfDay(now) - startOfDay(date)) / 86_400_000);
  if (diffDays === 0) {
    return 'TODAY';
  }
  if (diffDays === 1) {
    return 'YESTERDAY';
  }
  const day = date.getDate();
  const month = date.toLocaleString('en-GB', { month: 'long' }).toUpperCase();
  return `${day} ${month} ${date.getFullYear()}`;
}

function dayKey(iso: string): string {
  const date = new Date(iso);
  return `${date.getFullYear()}-${date.getMonth()}-${date.getDate()}`;
}

type ThreadMessageLike = {
  id: string;
  senderMemberId: string;
  createdAt: string;
};

export type ThreadListItem<M extends ThreadMessageLike = ThreadMessageLike> =
  | { kind: 'divider'; id: string; label: string }
  | { kind: 'message'; id: string; message: M; isFirstOfRun: boolean };

/**
 * Flattens a conversation's oldest-first messages into date dividers plus
 * grouped message runs (design handoff §"Message list" grouping rules).
 */
export function buildThreadItems<M extends ThreadMessageLike>(messages: ReadonlyArray<M>): ThreadListItem<M>[] {
  const items: ThreadListItem<M>[] = [];
  let lastDayKey: string | null = null;
  let lastAuthor: string | null = null;

  for (const message of messages) {
    const key = dayKey(message.createdAt);
    const showDivider = key !== lastDayKey;
    if (showDivider) {
      items.push({ kind: 'divider', id: `divider-${message.id}`, label: formatDateDividerLabel(message.createdAt) });
      lastAuthor = null;
    }
    items.push({
      kind: 'message',
      id: message.id,
      message,
      isFirstOfRun: showDivider || message.senderMemberId !== lastAuthor,
    });
    lastDayKey = key;
    lastAuthor = message.senderMemberId;
  }

  return items;
}
