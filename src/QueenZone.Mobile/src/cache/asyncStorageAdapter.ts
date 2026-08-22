import AsyncStorage from '@react-native-async-storage/async-storage';
import type { KeyValueStorage } from './storage';

/** Adapts React Native AsyncStorage to {@link KeyValueStorage}. */
export function createAsyncStorageAdapter(
  storage: typeof AsyncStorage = AsyncStorage,
): KeyValueStorage {
  return {
    getItem: (key) => storage.getItem(key),
    setItem: (key, value) => storage.setItem(key, value),
    removeItem: (key) => storage.removeItem(key),
    getAllKeys: () => storage.getAllKeys(),
    multiRemove: (keys) => storage.multiRemove([...keys]),
  };
}
