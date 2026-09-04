/**
 * Process-wide invalidation store (ADR 0018 decision 3).
 *
 * Keyed from `keys.ts`. Prefix subscribe + prefix invalidate replace the
 * per-resource epoch modules. ContentCache (JSON LRU) and the future #927
 * binary download store sit beside this module — they are not merged.
 *
 * #927 download-manifest UI state (`queued` / `downloading` / `downloaded` /
 * `failed` / `removing`) should live here under `DOWNLOAD_UI_CACHE_KEY_PREFIX`.
 * Binary files and the durable manifest stay in that sibling store.
 */

export type ExternalStoreListener = () => void;

export type ExternalStore = {
  getVersion(key: string): number;
  getPrefixVersion(prefix: string): number;
  subscribe(key: string, listener: ExternalStoreListener): () => void;
  subscribePrefix(prefix: string, listener: ExternalStoreListener): () => void;
  invalidate(key: string): void;
  invalidatePrefix(prefix: string): void;
};

function addListener(
  map: Map<string, Set<ExternalStoreListener>>,
  id: string,
  listener: ExternalStoreListener,
): () => void {
  let listeners = map.get(id);
  if (!listeners) {
    listeners = new Set();
    map.set(id, listeners);
  }
  listeners.add(listener);
  return () => {
    listeners.delete(listener);
    if (listeners.size === 0) {
      map.delete(id);
    }
  };
}

function notify(listeners: Iterable<ExternalStoreListener>): void {
  for (const listener of [...listeners]) {
    listener();
  }
}

function bump(map: Map<string, number>, id: string): void {
  map.set(id, (map.get(id) ?? 0) + 1);
}

export function createExternalStore(): ExternalStore {
  const keyVersions = new Map<string, number>();
  const prefixVersions = new Map<string, number>();
  const keyListeners = new Map<string, Set<ExternalStoreListener>>();
  const prefixListeners = new Map<string, Set<ExternalStoreListener>>();

  function getVersion(key: string): number {
    let version = keyVersions.get(key) ?? 0;
    for (const [prefix, prefixVersion] of prefixVersions) {
      if (key.startsWith(prefix)) {
        version += prefixVersion;
      }
    }
    return version;
  }

  function getPrefixVersion(prefix: string): number {
    let version = prefixVersions.get(prefix) ?? 0;
    for (const [key, keyVersion] of keyVersions) {
      if (key.startsWith(prefix)) {
        version += keyVersion;
      }
    }
    for (const [other, otherVersion] of prefixVersions) {
      if (other !== prefix && (other.startsWith(prefix) || prefix.startsWith(other))) {
        version += otherVersion;
      }
    }
    return version;
  }

  return {
    getVersion,
    getPrefixVersion,
    subscribe(key, listener) {
      return addListener(keyListeners, key, listener);
    },
    subscribePrefix(prefix, listener) {
      return addListener(prefixListeners, prefix, listener);
    },
    invalidate(key) {
      bump(keyVersions, key);
      notify(keyListeners.get(key) ?? []);
      for (const [prefix, listeners] of prefixListeners) {
        if (key.startsWith(prefix)) {
          notify(listeners);
        }
      }
    },
    invalidatePrefix(prefix) {
      bump(prefixVersions, prefix);
      for (const [other, listeners] of prefixListeners) {
        if (prefix.startsWith(other) || other.startsWith(prefix)) {
          notify(listeners);
        }
      }
      for (const [key, listeners] of keyListeners) {
        if (key.startsWith(prefix)) {
          notify(listeners);
        }
      }
    },
  };
}

let shared = createExternalStore();

export function getStoreVersion(key: string): number {
  return shared.getVersion(key);
}

export function getPrefixVersion(prefix: string): number {
  return shared.getPrefixVersion(prefix);
}

export function subscribe(key: string, listener: ExternalStoreListener): () => void {
  return shared.subscribe(key, listener);
}

export function subscribePrefix(prefix: string, listener: ExternalStoreListener): () => void {
  return shared.subscribePrefix(prefix, listener);
}

export function invalidate(key: string): void {
  shared.invalidate(key);
}

export function invalidatePrefix(prefix: string): void {
  shared.invalidatePrefix(prefix);
}

/** Test helper. Orphans any live subscribers from the previous instance. */
export function resetExternalStoreForTests(): void {
  shared = createExternalStore();
}
