import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { createMemoryStorage } from '../../cache/storage.ts';
import { createNewsShareStore, newsShareSlotKey } from './draftStore.ts';

const form = {
  v: 1 as const,
  kind: 'form' as const,
  draft: { url: 'https://example.com/story', title: 'Dates', notes: '', origin: 'share' as const },
};

describe('news share draft store', () => {
  it('persists and hydrates a form slot', async () => {
    const store = createNewsShareStore(createMemoryStorage());
    await store.write(form);
    assert.deepEqual(await store.read(), form);
  });

  it('clears corrupt JSON on read', async () => {
    const storage = createMemoryStorage({ [newsShareSlotKey]: '{not-json' });
    const store = createNewsShareStore(storage);
    assert.equal(await store.read(), null);
    assert.equal(await storage.getItem(newsShareSlotKey), null);
  });

  it('clears a slot that is not choose or form', async () => {
    const storage = createMemoryStorage({
      [newsShareSlotKey]: JSON.stringify({ v: 1, kind: 'submitting', draft: form.draft }),
    });
    const store = createNewsShareStore(storage);
    assert.equal(await store.read(), null);
    assert.equal(await storage.getItem(newsShareSlotKey), null);
  });

  it('leaves null after cancel and after a successful 201 clear', async () => {
    const store = createNewsShareStore(createMemoryStorage());
    await store.write(form);
    await store.clear();
    assert.equal(await store.read(), null);
  });
});
