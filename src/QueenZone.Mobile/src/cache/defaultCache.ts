import { createAsyncStorageAdapter } from './asyncStorageAdapter';
import { ContentCache } from './contentCache';

let shared: ContentCache | null = null;

/** Process-wide content cache backed by AsyncStorage (max 40 detail entries). */
export function getContentCache(): ContentCache {
  if (!shared) {
    shared = new ContentCache({
      storage: createAsyncStorageAdapter(),
      maxEntries: 40,
    });
  }
  return shared;
}

/** Test helper to replace or clear the singleton. */
export function setContentCacheForTests(cache: ContentCache | null): void {
  shared = cache;
}
