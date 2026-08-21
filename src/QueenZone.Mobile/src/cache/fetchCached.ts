import { fetchJson, type FetchJsonOptions } from '../api/client';
import { getContentCache } from './defaultCache';
import type { ContentCache } from './contentCache';
import { withOfflineCache } from './withOfflineCache';

export type FetchCachedOptions = FetchJsonOptions & {
  /** Stable key within the content cache (e.g. `news:42`). */
  cacheKey: string;
  /** Override the default AsyncStorage-backed cache (tests). */
  cache?: ContentCache;
};

/**
 * Network-first fetch with offline cache fallback for previously opened details.
 * Mirrors the PWA navigate strategy in `wwwroot/sw.js`.
 */
export async function fetchJsonWithOfflineCache<T>(
  path: string,
  options: FetchCachedOptions,
): Promise<T> {
  const { cacheKey, cache = getContentCache(), ...fetchOptions } = options;
  return withOfflineCache(cache, cacheKey, () => fetchJson<T>(path, fetchOptions));
}
