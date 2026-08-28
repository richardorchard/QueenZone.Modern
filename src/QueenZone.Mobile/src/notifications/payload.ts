/**
 * #757 push `data` contract. Deep-links use these keys only — do not invent
 * parallel per-platform or per-category shapes.
 *
 * | Key              | When                         |
 * | ---------------- | ---------------------------- |
 * | category         | forumReply \| privateMessage \| news |
 * | topicId          | category=forumReply          |
 * | postId           | optional, forumReply         |
 * | conversationId   | category=privateMessage      |
 * | articleId        | category=news                |
 */

export const notificationCategories = ['forumReply', 'privateMessage', 'news'] as const;

export type NotificationCategory = (typeof notificationCategories)[number];

export type NotificationDestination =
  | { category: 'forumReply'; topicId: number; postId?: number }
  | { category: 'privateMessage'; conversationId: string }
  | { category: 'news'; articleId: number };

const conversationIdPattern =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function unwrapData(data: unknown): Record<string, unknown> | null {
  if (typeof data === 'string') {
    const trimmed = data.trim();
    if (trimmed.length === 0) {
      return null;
    }
    try {
      const parsed: unknown = JSON.parse(trimmed);
      return isRecord(parsed) ? parsed : null;
    } catch {
      return null;
    }
  }

  return isRecord(data) ? data : null;
}

function isNotificationCategory(value: string): value is NotificationCategory {
  return (notificationCategories as readonly string[]).includes(value);
}

function readCategory(value: unknown): NotificationCategory | null {
  if (typeof value !== 'string') {
    return null;
  }
  const category = value.trim();
  return isNotificationCategory(category) ? category : null;
}

function readPositiveInt(value: unknown): number | null {
  if (typeof value === 'number') {
    if (!Number.isInteger(value) || value <= 0) {
      return null;
    }
    return value;
  }

  if (typeof value !== 'string') {
    return null;
  }

  const parsed = Number.parseInt(value.trim(), 10);
  if (!Number.isFinite(parsed) || parsed <= 0 || !Number.isInteger(parsed)) {
    return null;
  }
  return parsed;
}

function readConversationId(value: unknown): string | null {
  if (typeof value !== 'string') {
    return null;
  }
  const id = value.trim();
  return conversationIdPattern.test(id) ? id : null;
}

/**
 * Maps a notification `data` dictionary to a screen destination, or null if it is not actionable.
 * Accepts FCM `message.data` and an iOS APNs dictionary with the same #757 keys beside `aps`.
 */
export function parseNotificationData(data: unknown): NotificationDestination | null {
  const record = unwrapData(data);
  if (!record) {
    return null;
  }

  const category = readCategory(record.category);
  if (!category) {
    return null;
  }

  if (category === 'forumReply') {
    const topicId = readPositiveInt(record.topicId);
    if (topicId === null) {
      return null;
    }
    const postId = readPositiveInt(record.postId);
    return postId === null ? { category, topicId } : { category, topicId, postId };
  }

  if (category === 'privateMessage') {
    const conversationId = readConversationId(record.conversationId);
    return conversationId === null ? null : { category, conversationId };
  }

  const articleId = readPositiveInt(record.articleId);
  return articleId === null ? null : { category, articleId };
}

export function fallbackNoticeCopy(destination: NotificationDestination): { title: string; body: string } {
  switch (destination.category) {
    case 'forumReply':
      return { title: 'New forum reply', body: 'New reply' };
    case 'privateMessage':
      return { title: 'New private message', body: 'You have a new message.' };
    case 'news':
      return { title: 'New QueenZone article', body: 'New article published.' };
    default: {
      const _exhaustive: never = destination;
      return _exhaustive;
    }
  }
}

export function noticeEyebrow(category: NotificationCategory): string {
  switch (category) {
    case 'forumReply':
      return 'Forum';
    case 'privateMessage':
      return 'Message';
    case 'news':
      return 'News';
    default: {
      const _exhaustive: never = category;
      return _exhaustive;
    }
  }
}
