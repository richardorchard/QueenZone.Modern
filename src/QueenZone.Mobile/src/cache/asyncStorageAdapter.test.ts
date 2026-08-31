import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { createAsyncStorageAdapter } from './asyncStorageAdapter.ts';

function createFakeAsyncStorage(initial: Record<string, string> = {}) {
  const map = new Map<string, string>(Object.entries(initial));
  const calls = {
    getItem: [] as string[],
    setItem: [] as [string, string][],
    removeItem: [] as string[],
    getAllKeys: 0,
    multiRemove: [] as string[][],
  };

  return {
    calls,
    getItem: async (key: string) => {
      calls.getItem.push(key);
      return map.has(key) ? map.get(key)! : null;
    },
    setItem: async (key: string, value: string) => {
      calls.setItem.push([key, value]);
      map.set(key, value);
    },
    removeItem: async (key: string) => {
      calls.removeItem.push(key);
      map.delete(key);
    },
    getAllKeys: async () => {
      calls.getAllKeys += 1;
      return [...map.keys()];
    },
    multiRemove: async (keys: readonly string[]) => {
      calls.multiRemove.push([...keys]);
      for (const key of keys) {
        map.delete(key);
      }
    },
  };
}

describe('createAsyncStorageAdapter', () => {
  it('passes get/set/remove/getAllKeys/multiRemove through to AsyncStorage', async () => {
    const storage = createFakeAsyncStorage({ keep: '1' });
    const adapter = createAsyncStorageAdapter(storage as never);

    await adapter.setItem('news:1', '{"id":1}');
    assert.equal(await adapter.getItem('news:1'), '{"id":1}');
    assert.deepEqual(await adapter.getAllKeys(), ['keep', 'news:1']);

    await adapter.removeItem('keep');
    assert.equal(await adapter.getItem('keep'), null);

    await adapter.multiRemove(['news:1']);
    assert.equal(await adapter.getItem('news:1'), null);
    assert.deepEqual(await adapter.getAllKeys(), []);

    assert.deepEqual(storage.calls.setItem, [['news:1', '{"id":1}']]);
    assert.deepEqual(storage.calls.getItem, ['news:1', 'keep', 'news:1']);
    assert.deepEqual(storage.calls.removeItem, ['keep']);
    assert.deepEqual(storage.calls.multiRemove, [['news:1']]);
    assert.equal(storage.calls.getAllKeys, 2);
  });
});
