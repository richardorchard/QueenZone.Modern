import assert from 'node:assert/strict';
import { register } from 'node:module';
import { afterEach, describe, it } from 'node:test';
import { pathToFileURL } from 'node:url';

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
const { getContentCache, setContentCacheForTests } = await import('./defaultCache.ts');
const { createMemoryStorage } = await import('./storage.ts');

describe('getContentCache', () => {
  afterEach(() => {
    setContentCacheForTests(null);
  });

  it('returns the same singleton after the first create', () => {
    setContentCacheForTests(null);
    const first = getContentCache();
    const second = getContentCache();
    assert.equal(first, second);
    assert.ok(first instanceof ContentCache);
  });

  it('lets tests replace and reset the singleton', async () => {
    const injected = new ContentCache({ storage: createMemoryStorage() });
    await injected.put('news:1', { id: 1 });
    setContentCacheForTests(injected);
    assert.equal(getContentCache(), injected);
    assert.deepEqual(await getContentCache().get('news:1'), { id: 1 });

    const replacement = new ContentCache({ storage: createMemoryStorage() });
    setContentCacheForTests(replacement);
    assert.equal(getContentCache(), replacement);
    assert.equal(await getContentCache().get('news:1'), null);

    setContentCacheForTests(null);
    const created = getContentCache();
    assert.notEqual(created, replacement);
    assert.notEqual(created, injected);
  });
});
