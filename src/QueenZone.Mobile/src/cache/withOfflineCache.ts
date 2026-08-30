import type { ContentCache } from './contentCache';

export type CacheSource = 'network' | 'cache';

export type CachedResult<T> = {
  data: T;
  source: CacheSource;
  cachedAt: string;
};

export type OfflineCacheOptions = {
  /**
   * HTTP statuses that delete the cached key before rethrowing.
   * Online 401/403/404 are authoritative for private data; 404 for public topics.
   */
  invalidateOn?: readonly number[];
  /** Network-only (pull-to-refresh): write-through on success, never serve cache. */
  fallback?: boolean;
};

function isAbortError(err: unknown): boolean {
  return err instanceof Error && err.name === 'AbortError';
}

function apiKind(err: unknown): unknown {
  return err instanceof Error && err.name === 'ApiError' && 'kind' in err
    ? (err as { kind: unknown }).kind
    : undefined;
}

function isCacheFallbackFailure(err: unknown): boolean {
  const kind = apiKind(err);
  return kind === 'offline' || kind === 'timeout';
}

function httpStatus(err: unknown): number | null {
  if (!(err instanceof Error) || err.name !== 'ApiError' || !('status' in err)) {
    return null;
  }
  const status = (err as { status: unknown }).status;
  return typeof status === 'number' ? status : null;
}

/**
 * Network-first with optional offline/timeout cache fallback.
 * Returns provenance so screens can show “Offline · last updated …”.
 */
export async function withOfflineCacheResult<T>(
  cache: ContentCache,
  cacheKey: string,
  fetchFresh: () => Promise<T>,
  options: OfflineCacheOptions = {},
): Promise<CachedResult<T>> {
  const fallback = options.fallback !== false;
  const invalidateOn = options.invalidateOn ?? [];

  try {
    const data = await fetchFresh();
    let cachedAt = new Date().toISOString();
    try {
      cachedAt = await cache.put(cacheKey, data);
    } catch {
      // Read path still succeeds if the device store is full or unavailable.
    }
    return { data, source: 'network', cachedAt };
  } catch (err) {
    if (isAbortError(err)) {
      throw err;
    }

    const status = httpStatus(err);
    if (status !== null && invalidateOn.includes(status)) {
      try {
        await cache.remove(cacheKey);
      } catch {}
      throw err;
    }

    if (!fallback || !isCacheFallbackFailure(err)) {
      throw err;
    }

    try {
      const cached = await cache.read<T>(cacheKey);
      if (cached !== null) {
        return { data: cached.payload, source: 'cache', cachedAt: cached.cachedAt };
      }
    } catch {}
    throw err;
  }
}

/**
 * Convenience wrapper for screens that do not need provenance
 * (news / biography / discography details).
 */
export async function withOfflineCache<T>(
  cache: ContentCache,
  cacheKey: string,
  fetchFresh: () => Promise<T>,
  options?: OfflineCacheOptions,
): Promise<T> {
  const result = await withOfflineCacheResult(cache, cacheKey, fetchFresh, options);
  return result.data;
}
