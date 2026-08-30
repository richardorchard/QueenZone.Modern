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
const { getMessagesCache, setMessagesCacheForTests } = await import('./messagesCache.ts');
const { createMemoryStorage } = await import('./storage.ts');

describe('getMessagesCache', () => {
  afterEach(() => {
    setMessagesCacheForTests(null);
  });

  it('returns the same singleton after the first create', () => {
    setMessagesCacheForTests(null);
    const first = getMessagesCache();
    const second = getMessagesCache();
    assert.equal(first, second);
    assert.ok(first instanceof ContentCache);
  });

  it('lets tests replace and reset the singleton', async () => {
    const injected = new ContentCache({ storage: createMemoryStorage() });
    await injected.put('inbox:member-1', [{ conversationId: 'c1' }]);
    setMessagesCacheForTests(injected);
    assert.equal(getMessagesCache(), injected);
    assert.deepEqual(await getMessagesCache().get('inbox:member-1'), [{ conversationId: 'c1' }]);

    const replacement = new ContentCache({ storage: createMemoryStorage() });
    setMessagesCacheForTests(replacement);
    assert.equal(getMessagesCache(), replacement);
    assert.equal(await getMessagesCache().get('inbox:member-1'), null);

    setMessagesCacheForTests(null);
    const created = getMessagesCache();
    assert.notEqual(created, replacement);
    assert.notEqual(created, injected);
  });
});
