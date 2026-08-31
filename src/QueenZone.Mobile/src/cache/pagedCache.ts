import type { ContentCache } from './contentCache';
import { forumTopicPostsCacheKey, forumTopicPostsKeyPrefix } from './keys';

type PagedPosts = {
  items: { id: number }[];
  totalCount: number;
  totalPages: number;
};

function itemId(item: { id: number } | undefined): number | null {
  return item && typeof item.id === 'number' ? item.id : null;
}

/**
 * Page-1 tip/order/`totalCount` fingerprint. A later cached tail is incompatible
 * when any of these change — do not append a fresh first page onto a stale tail.
 */
export function pagedPostsFingerprint(page: PagedPosts): string {
  const first = itemId(page.items[0]);
  const last = itemId(page.items[page.items.length - 1]);
  return `${page.totalCount}:${page.totalPages}:${first}:${last}`;
}

export function pagedTailIncompatible(previous: PagedPosts, next: PagedPosts): boolean {
  return pagedPostsFingerprint(previous) !== pagedPostsFingerprint(next);
}

/** Drop cached posts pages after page 1 when the server page-1 snapshot moved. */
export async function invalidateIncompatiblePostPages(
  cache: ContentCache,
  topicId: number,
  nextPage1: PagedPosts,
): Promise<void> {
  const previous = await cache.get<PagedPosts>(forumTopicPostsCacheKey(topicId, 1));
  if (!previous || !pagedTailIncompatible(previous, nextPage1)) {
    return;
  }

  const prefix = forumTopicPostsKeyPrefix(topicId);
  const keys = await cache.listCacheKeys();
  const tail = keys.filter((key) => {
    if (!key.startsWith(prefix)) {
      return false;
    }
    const page = Number.parseInt(key.slice(prefix.length), 10);
    return Number.isInteger(page) && page > 1;
  });
  await Promise.all(tail.map((key) => cache.remove(key)));
}
