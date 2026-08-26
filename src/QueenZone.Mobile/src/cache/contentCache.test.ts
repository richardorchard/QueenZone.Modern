import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { ApiError } from '../api/errors.ts';
import { ContentCache } from './contentCache.ts';
import { withOfflineCache } from './withOfflineCache.ts';
import { createMemoryStorage } from './storage.ts';

describe('ContentCache', () => {
  it('stores and returns payloads', async () => {
    const cache = new ContentCache({ storage: createMemoryStorage() });
    await cache.put('news:1', { id: 1, title: 'Hello' });
    assert.deepEqual(await cache.get('news:1'), { id: 1, title: 'Hello' });
  });

  it('returns null for missing keys', async () => {
    const cache = new ContentCache({ storage: createMemoryStorage() });
    assert.equal(await cache.get('missing'), null);
  });

  it('evicts least-recently-accessed entries when over max', async () => {
    const cache = new ContentCache({ storage: createMemoryStorage(), maxEntries: 2 });
    await cache.put('a', { n: 1 });
    await cache.put('b', { n: 2 });
    // Touch `a` so `b` is older for eviction after a third put.
    assert.deepEqual(await cache.get('a'), { n: 1 });
    await cache.put('c', { n: 3 });

    assert.equal(await cache.size(), 2);
    assert.deepEqual(await cache.get('a'), { n: 1 });
    assert.deepEqual(await cache.get('c'), { n: 3 });
    assert.equal(await cache.get('b'), null);
  });

  it('drops corrupt entries', async () => {
    const storage = createMemoryStorage({
      'qz:content:bad': 'not-json',
    });
    const cache = new ContentCache({ storage });
    assert.equal(await cache.get('bad'), null);
    assert.equal(await storage.getItem('qz:content:bad'), null);
  });

  it('clears all prefixed entries', async () => {
    const storage = createMemoryStorage({ other: 'keep' });
    const cache = new ContentCache({ storage });
    await cache.put('news:1', { id: 1 });
    await cache.clear();
    assert.equal(await cache.size(), 0);
    assert.equal(await storage.getItem('other'), 'keep');
  });
});

describe('withOfflineCache', () => {
  it('writes through on successful fetch', async () => {
    const cache = new ContentCache({ storage: createMemoryStorage() });
    const result = await withOfflineCache(cache, 'news:1', async () => ({ id: 1, title: 'fresh' }));
    assert.deepEqual(result, { id: 1, title: 'fresh' });
    assert.deepEqual(await cache.get('news:1'), { id: 1, title: 'fresh' });
  });

  it('serves the last cached payload when the network is unreachable', async () => {
    const cache = new ContentCache({ storage: createMemoryStorage() });
    await cache.put('news:1', { id: 1, title: 'cached' });

    const result = await withOfflineCache(cache, 'news:1', async () => {
      throw new ApiError(0, 'Unable to reach QueenZone. Check your connection and try again.');
    });

    assert.deepEqual(result, { id: 1, title: 'cached' });
  });

  it('does not fall back for a timeout', async () => {
    const cache = new ContentCache({ storage: createMemoryStorage() });
    await cache.put('news:1', { id: 1, title: 'cached' });

    await assert.rejects(
      () =>
        withOfflineCache(cache, 'news:1', async () => {
          throw ApiError.timeout();
        }),
      (err: unknown) => err instanceof ApiError && err.kind === 'timeout',
    );
    assert.deepEqual(await cache.get('news:1'), { id: 1, title: 'cached' });
  });

  it('does not fall back for HTTP errors', async () => {
    const cache = new ContentCache({ storage: createMemoryStorage() });
    await cache.put('news:1', { id: 1, title: 'cached' });

    await assert.rejects(
      () =>
        withOfflineCache(cache, 'news:1', async () => {
          throw new ApiError(404, 'Not found.');
        }),
      (err: unknown) => err instanceof ApiError && err.status === 404,
    );
  });

  it('rethrows when offline and nothing is cached', async () => {
    const cache = new ContentCache({ storage: createMemoryStorage() });

    await assert.rejects(
      () =>
        withOfflineCache(cache, 'news:1', async () => {
          throw new ApiError(0, 'Unable to reach QueenZone.');
        }),
      (err: unknown) => err instanceof ApiError && err.status === 0,
    );
  });

  it('rethrows abort errors without reading the cache', async () => {
    const cache = new ContentCache({ storage: createMemoryStorage() });
    await cache.put('news:1', { id: 1, title: 'cached' });
    const abort = new Error('Aborted');
    abort.name = 'AbortError';

    await assert.rejects(
      () => withOfflineCache(cache, 'news:1', async () => {
        throw abort;
      }),
      (err: unknown) => err instanceof Error && err.name === 'AbortError',
    );
  });
});
