/** Public forum topic header. Survives sign-out. */
export function forumTopicCacheKey(topicId: number): string {
  return `forum:topic:${topicId}`;
}

/** One successfully opened posts page. Survives sign-out. */
export function forumTopicPostsCacheKey(topicId: number, page: number): string {
  return `forum:topic:${topicId}:posts:${page}`;
}

export function forumTopicPostsKeyPrefix(topicId: number): string {
  return `forum:topic:${topicId}:posts:`;
}

/**
 * Private conversation snapshot. Includes signed-in `memberId`, never the
 * Bearer token. Purged on sign-out and member switch.
 */
export function conversationCacheKey(memberId: string, conversationId: string): string {
  return `messages:member:${memberId}:conversation:${conversationId}`;
}

/**
 * Inbox first-page snapshot, so the list can render instantly on cold start
 * while a fresh fetch runs in the background. Purged on sign-out and member
 * switch, same as {@link conversationCacheKey}.
 */
export function inboxCacheKey(memberId: string): string {
  return `messages:member:${memberId}:inbox`;
}

export function privateMemberCachePrefix(memberId: string): string {
  return `messages:member:${memberId}:`;
}

/** All member-scoped private snapshots. */
export const PRIVATE_CACHE_KEY_PREFIX = 'messages:member:';
