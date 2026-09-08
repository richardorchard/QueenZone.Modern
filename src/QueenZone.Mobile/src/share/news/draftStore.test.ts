import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { createMemoryStorage } from '../../cache/storage.ts';
import { createNewsShareStore, newsShareConsumedKey, newsShareSlotKey } from './draftStore.ts';

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

  it('round-trips savedAt and keeps last consumed after the slot is cleared', async () => {
    const store = createNewsShareStore(createMemoryStorage());
    const stamped = { ...form, savedAt: '2026-09-08T12:00:00.000Z' };
    await store.write(stamped);
    await store.writeLastConsumed('https://example.com/story');
    assert.deepEqual(await store.read(), stamped);
    await store.clear();
    assert.equal(await store.read(), null);
    assert.equal(await store.readLastConsumed(), 'https://example.com/story');
  });

  it('drops a non-string savedAt but still returns the slot', async () => {
    const storage = createMemoryStorage({
      [newsShareSlotKey]: JSON.stringify({ ...form, savedAt: 99 }),
    });
    const store = createNewsShareStore(storage);
    assert.deepEqual(await store.read(), form);
  });

  it('treats a blank consumed fingerprint as missing', async () => {
    const storage = createMemoryStorage({ [newsShareConsumedKey]: '   ' });
    const store = createNewsShareStore(storage);
    assert.equal(await store.readLastConsumed(), null);
  });
});
