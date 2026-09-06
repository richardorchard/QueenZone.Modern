import { useCallback, useEffect, useRef, useSyncExternalStore } from 'react';
import { getPrefixVersion, getStoreVersion, subscribe, subscribePrefix } from './externalStore';

/**
 * Version for one `keys.ts` entry. Re-renders when that key is invalidated
 * directly or via a matching prefix. Subscribe/getSnapshot are keyed so a
 * recycled list row does not keep listening to the first item's key.
 */
export function useStoreVersion(key: string): number {
  const subscribeToKey = useCallback((onStoreChange: () => void) => subscribe(key, onStoreChange), [key]);
  const getSnapshot = useCallback(() => getStoreVersion(key), [key]);
  return useSyncExternalStore(subscribeToKey, getSnapshot, getSnapshot);
}

/**
 * Version for a `keys.ts` prefix. Re-renders when any overlapping prefix or
 * matching key is invalidated.
 */
export function usePrefixVersion(prefix: string): number {
  const subscribeToPrefix = useCallback(
    (onStoreChange: () => void) => subscribePrefix(prefix, onStoreChange),
    [prefix],
  );
  const getSnapshot = useCallback(() => getPrefixVersion(prefix), [prefix]);
  return useSyncExternalStore(subscribeToPrefix, getSnapshot, getSnapshot);
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
