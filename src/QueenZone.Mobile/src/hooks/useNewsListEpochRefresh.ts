import { useEffect, useRef } from 'react';
import { subscribeNewsListEpoch } from '../notifications/newsListEpoch';

/**
 * Calls refresh() when a news push bumps the list epoch. Unmount removes the
 * listener, so an unmounted Home news section or NewsIndex does not fetch.
 */
export function useNewsListEpochRefresh(refresh: () => void | Promise<void>): void {
  const refreshRef = useRef(refresh);
  refreshRef.current = refresh;

  useEffect(
    () =>
      subscribeNewsListEpoch(() => {
        void refreshRef.current();
      }),
    [],
  );
}
