import { fetchJson, type FetchJsonOptions } from '../api/client';
import { getContentCache } from './defaultCache';
import type { ContentCache } from './contentCache';
import { withOfflineCacheResult, type CachedResult, type OfflineCacheOptions } from './withOfflineCache';

export type FetchCachedOptions = FetchJsonOptions &
  OfflineCacheOptions & {
    /** Stable key within the content cache (e.g. `news:42`). */
    cacheKey: string;
    /** Override the default AsyncStorage-backed cache (tests). */
    cache?: ContentCache;
  };

const inFlight = new Map<string, Promise<CachedResult<unknown>>>();

/**
 * Network-first fetch with offline cache fallback for previously opened details.
 * Mirrors the PWA navigate strategy in `wwwroot/sw.js`.
 * Concurrent callers with the same `cacheKey` share one in-flight promise.
 */
export async function fetchJsonWithOfflineCache<T>(
  path: string,
  options: FetchCachedOptions,
): Promise<T> {
  const result = await fetchJsonWithOfflineCacheResult<T>(path, options);
  return result.data;
}

export async function fetchJsonWithOfflineCacheResult<T>(
  path: string,
  options: FetchCachedOptions,
): Promise<CachedResult<T>> {
  const { cacheKey, cache = getContentCache(), invalidateOn, fallback, ...fetchOptions } = options;

  const existing = inFlight.get(cacheKey);
  if (existing) {
    return existing as Promise<CachedResult<T>>;
  }

  const pending = withOfflineCacheResult(cache, cacheKey, () => fetchJson<T>(path, fetchOptions), {
    invalidateOn,
    fallback,
  }).finally(() => {
    if (inFlight.get(cacheKey) === pending) {
      inFlight.delete(cacheKey);
    }
  });

  inFlight.set(cacheKey, pending);
  return pending;
}

export type { CachedResult };
