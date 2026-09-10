import assert from 'node:assert/strict';
import { register } from 'node:module';
import { describe, it, mock } from 'node:test';
import { pathToFileURL } from 'node:url';
import { ApiError } from '../api/errors.ts';

register(
  `data:text/javascript,${encodeURIComponent(`
    export async function resolve(specifier, context, nextResolve) {
      if (specifier.startsWith('.') && !/\\\\.(?:[cm]?[jt]s|json)$/.test(specifier)) {
        try {
          return await nextResolve(specifier + '.ts', context);
        } catch {
          return nextResolve(specifier, context);
        }
      }
      return nextResolve(specifier, context);
    }
  `)}`,
  pathToFileURL('./'),
);

const { ContentCache } = await import('./contentCache.ts');
const { createMemoryStorage } = await import('./storage.ts');
const { withOfflineCacheResult } = await import('./withOfflineCache.ts');

function newCache() {
  return new ContentCache({ storage: createMemoryStorage() });
}

// Background revalidation is fire-and-forget; flush the microtask queue so
// its `.then`/`.catch` chain settles before assertions run.
function flush(): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, 0));
}

describe('withOfflineCacheResult TTL / stale-while-revalidate', () => {
  it('goes to the network when there is no cached entry, even with a ttlMs set', async () => {
    const cache = newCache();
    const fetchFresh = mock.fn(async () => ({ value: 'fresh' }));

    const result = await withOfflineCacheResult(cache, 'k', fetchFresh, { ttlMs: 60_000 });

    assert.deepEqual(result, { data: { value: 'fresh' }, source: 'network', cachedAt: result.cachedAt });
    assert.equal(fetchFresh.mock.calls.length, 1);
  });

  it('serves the cached value immediately without waiting on the network', async () => {
    const cache = newCache();
    await cache.put('k', { value: 'cached' });
    let releaseFetch!: (value: { value: string }) => void;
    const fetchFresh = mock.fn(
      () =>
        new Promise<{ value: string }>((resolve) => {
          releaseFetch = resolve;
        }),
    );

    const result = await withOfflineCacheResult(cache, 'k', fetchFresh, { ttlMs: 60_000 });

    assert.equal(result.source, 'cache');
    assert.deepEqual(result.data, { value: 'cached' });

    releaseFetch({ value: 'revalidated' });
    await flush();
  });

  it('revalidates in the background and updates the cache for the next read', async () => {
    const cache = newCache();
    await cache.put('k', { value: 'cached' });
    const fetchFresh = mock.fn(async () => ({ value: 'revalidated' }));

    const result = await withOfflineCacheResult(cache, 'k', fetchFresh, { ttlMs: 60_000 });
    assert.equal(result.source, 'cache');

    await flush();

    assert.equal(fetchFresh.mock.calls.length, 1);
    assert.deepEqual(await cache.get('k'), { value: 'revalidated' });
  });

  it('goes to the network once the cached entry is older than ttlMs', async () => {
    const cache = newCache();
    await cache.put('k', { value: 'stale' });
    const fetchFresh = mock.fn(async () => ({ value: 'fresh' }));

    const result = await withOfflineCacheResult(cache, 'k', fetchFresh, { ttlMs: -1 });

    assert.equal(result.source, 'network');
    assert.deepEqual(result.data, { value: 'fresh' });
    assert.equal(fetchFresh.mock.calls.length, 1);
  });

  it('never short-circuits on the network-only pull-to-refresh path (fallback: false)', async () => {
    const cache = newCache();
    await cache.put('k', { value: 'cached' });
    const fetchFresh = mock.fn(async () => ({ value: 'fresh' }));

    const result = await withOfflineCacheResult(cache, 'k', fetchFresh, {
      ttlMs: 60_000,
      fallback: false,
    });

    assert.equal(result.source, 'network');
    assert.equal(fetchFresh.mock.calls.length, 1);
  });

  it('deduplicates concurrent background revalidations for the same key', async () => {
    const cache = newCache();
    await cache.put('k', { value: 'cached' });
    const fetchFresh = mock.fn(async () => ({ value: 'revalidated' }));

    await withOfflineCacheResult(cache, 'k', fetchFresh, { ttlMs: 60_000 });
    await withOfflineCacheResult(cache, 'k', fetchFresh, { ttlMs: 60_000 });

    await flush();

    assert.equal(fetchFresh.mock.calls.length, 1);
  });

  it('swallows a failed background revalidation and keeps the stale value cached', async () => {
    const cache = newCache();
    await cache.put('k', { value: 'cached' });
    const fetchFresh = mock.fn(async () => {
      throw ApiError.offline();
    });

    const result = await withOfflineCacheResult(cache, 'k', fetchFresh, { ttlMs: 60_000 });
    assert.equal(result.source, 'cache');

    await flush();

    assert.deepEqual(await cache.get('k'), { value: 'cached' });
  });

  it('removes the cached entry when a background revalidation hits an invalidateOn status', async () => {
    const cache = newCache();
    await cache.put('k', { value: 'cached' });
    const fetchFresh = mock.fn(async () => {
      throw ApiError.http(401, 'Unauthorized.');
    });

    await withOfflineCacheResult(cache, 'k', fetchFresh, { ttlMs: 60_000, invalidateOn: [401] });

    await flush();

    assert.equal(await cache.get('k'), null);
  });

  it('allows a new background revalidation after the previous one settles', async () => {
    const cache = newCache();
    await cache.put('k', { value: 'cached' });
    const fetchFresh = mock.fn(async () => ({ value: 'revalidated' }));

    await withOfflineCacheResult(cache, 'k', fetchFresh, { ttlMs: 60_000 });
    await flush();
    await withOfflineCacheResult(cache, 'k', fetchFresh, { ttlMs: 60_000 });
    await flush();

    assert.equal(fetchFresh.mock.calls.length, 2);
  });
});
