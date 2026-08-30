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

export function privateMemberCachePrefix(memberId: string): string {
  return `messages:member:${memberId}:`;
}

/** All member-scoped private snapshots. */
export const PRIVATE_CACHE_KEY_PREFIX = 'messages:member:';
