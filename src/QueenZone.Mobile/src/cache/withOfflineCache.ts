import type { ContentCache } from './contentCache';

function isOfflineFailure(err: unknown): boolean {
  return (
    err instanceof Error &&
    err.name === 'ApiError' &&
    'kind' in err &&
    (err as { kind: unknown }).kind === 'offline'
  );
}

/**
 * Network-first with offline cache fallback.
 * On success, refreshes the cache. On offline failure, returns the last
 * cached payload when present. Timeout and HTTP miss the cache.
 */
export async function withOfflineCache<T>(
  cache: ContentCache,
  cacheKey: string,
  fetchFresh: () => Promise<T>,
): Promise<T> {
  try {
    const data = await fetchFresh();
    try {
      await cache.put(cacheKey, data);
    } catch {
      // Cache write failures must not break online reading.
    }
    return data;
  } catch (err) {
    if (err instanceof Error && err.name === 'AbortError') {
      throw err;
    }
    if (!isOfflineFailure(err)) {
      throw err;
    }

    try {
      const cached = await cache.get<T>(cacheKey);
      if (cached !== null) {
        return cached;
      }
    } catch {
      // Fall through to the original network error.
    }
    throw err;
  }
}
