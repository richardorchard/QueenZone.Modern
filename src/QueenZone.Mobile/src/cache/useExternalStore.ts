import { useEffect, useRef, useSyncExternalStore } from 'react';
import { getPrefixVersion, getStoreVersion, subscribe, subscribePrefix } from './externalStore';

/**
 * Version for one `keys.ts` entry. Re-renders when that key is invalidated
 * directly or via a matching prefix.
 */
export function useStoreVersion(key: string): number {
  return useSyncExternalStore(
    (onStoreChange) => subscribe(key, onStoreChange),
    () => getStoreVersion(key),
    () => getStoreVersion(key),
  );
}

/**
 * Version for a `keys.ts` prefix. Re-renders when any overlapping prefix or
 * matching key is invalidated.
 */
export function usePrefixVersion(prefix: string): number {
  return useSyncExternalStore(
    (onStoreChange) => subscribePrefix(prefix, onStoreChange),
    () => getPrefixVersion(prefix),
    () => getPrefixVersion(prefix),
  );
}

/**
 * Calls `refresh` when `key` is invalidated while mounted. The generation
 * current at mount does not fire — same contract as the retired news-list
 * epoch hook.
 */
export function useStoreRefresh(key: string, refresh: () => void | Promise<void>): void {
  const refreshRef = useRef(refresh);
  refreshRef.current = refresh;
  const version = useStoreVersion(key);
  const seenVersion = useRef(version);

  useEffect(() => {
    if (version === seenVersion.current) {
      return;
    }
    seenVersion.current = version;
    void refreshRef.current();
  }, [version]);
}
