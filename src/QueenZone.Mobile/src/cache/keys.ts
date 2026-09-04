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

/**
 * Public news-list invalidation key (ADR 0018 decision 3). Not a ContentCache
 * entry — Home news and NewsIndex subscribe to this key.
 */
export const NEWS_LIST_CACHE_KEY = 'news:list';

/** Prefix for public news invalidation keys. Survives sign-out. */
export const NEWS_CACHE_KEY_PREFIX = 'news:';

/**
 * Process-wide private-message unread signal. Lives under
 * {@link PRIVATE_CACHE_KEY_PREFIX} so `SessionContext.clearLocal` /
 * `purgePrivateContentCache` prefix-invalidates it with other member data.
 */
export const PM_UNREAD_CACHE_KEY = `${PRIVATE_CACHE_KEY_PREFIX}unread`;

/**
 * #927 download-manifest UI lifecycle (`queued` / `downloading` /
 * `downloaded` / `failed` / `removing`) belongs on `externalStore` under
 * this prefix so listing / detail / Play All share one subscription.
 *
 * Binary files and the durable download manifest are a **sibling store**
 * beside ContentCache (ADR 0018 decision 4) — do not put bytes here, and
 * do not merge that store into ContentCache. This tree is the seam only;
 * #927 implements the states.
 */
export const DOWNLOAD_UI_CACHE_KEY_PREFIX = 'downloads:member:';

export function downloadUiCachePrefix(memberId: string): string {
  return `${DOWNLOAD_UI_CACHE_KEY_PREFIX}${memberId}:`;
}

export function downloadUiCacheKey(memberId: string, performanceId: string): string {
  return `${downloadUiCachePrefix(memberId)}performance:${performanceId}`;
}
