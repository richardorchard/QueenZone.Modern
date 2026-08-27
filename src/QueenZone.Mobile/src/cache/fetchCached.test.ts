import assert from 'node:assert/strict';
import { register } from 'node:module';
import { afterEach, describe, it, mock } from 'node:test';
import { pathToFileURL } from 'node:url';
import { ApiError } from '../api/errors.ts';

const fetchJsonMock = mock.fn();
(globalThis as { __qzFetchJsonMock?: typeof fetchJsonMock }).__qzFetchJsonMock = fetchJsonMock;

register(
  `data:text/javascript,${encodeURIComponent(`
    export async function resolve(specifier, context, nextResolve) {
      if (specifier === '../api/client') {
        return {
          url: 'data:text/javascript,export async function fetchJson(...args){return globalThis.__qzFetchJsonMock(...args)}',
          shortCircuit: true,
        };
      }
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
const { fetchJsonWithOfflineCache } = await import('./fetchCached.ts');

describe('fetchJsonWithOfflineCache', () => {
  afterEach(() => {
    fetchJsonMock.mock.resetCalls();
  });

  it('writes a successful fetch through to the injected cache', async () => {
    const cache = new ContentCache({ storage: createMemoryStorage() });
    fetchJsonMock.mock.mockImplementation(async () => ({ id: 42, title: 'fresh' }));

    const result = await fetchJsonWithOfflineCache('/content/news/42', {
      cacheKey: 'news:42',
      cache,
      accessToken: 'tok',
    });

    assert.deepEqual(result, { id: 42, title: 'fresh' });
    assert.deepEqual(await cache.get('news:42'), { id: 42, title: 'fresh' });
    assert.equal(fetchJsonMock.mock.calls.length, 1);
    assert.deepEqual(fetchJsonMock.mock.calls[0]?.arguments, ['/content/news/42', { accessToken: 'tok' }]);
  });

  it('serves the cached payload when fetchJson throws an offline ApiError', async () => {
    const cache = new ContentCache({ storage: createMemoryStorage() });
    await cache.put('news:42', { id: 42, title: 'cached' });
    fetchJsonMock.mock.mockImplementation(async () => {
      throw ApiError.offline();
    });

    const result = await fetchJsonWithOfflineCache('/content/news/42', {
      cacheKey: 'news:42',
      cache,
    });

    assert.deepEqual(result, { id: 42, title: 'cached' });
    assert.equal(fetchJsonMock.mock.calls.length, 1);
  });
});
