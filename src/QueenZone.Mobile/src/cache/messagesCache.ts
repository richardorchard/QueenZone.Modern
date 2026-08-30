import { createAsyncStorageAdapter } from './asyncStorageAdapter';
import { ContentCache } from './contentCache';

let shared: ContentCache | null = null;

/** Process-wide cache for the private-messages inbox snapshot (max 20 entries: one per recently signed-in member). */
export function getMessagesCache(): ContentCache {
  if (!shared) {
    shared = new ContentCache({
      storage: createAsyncStorageAdapter(),
      maxEntries: 20,
      keyPrefix: 'qz:messages:',
    });
  }
  return shared;
}

/** Test helper to replace or clear the singleton. */
export function setMessagesCacheForTests(cache: ContentCache | null): void {
  shared = cache;
}
