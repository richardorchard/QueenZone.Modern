import { useCallback, useState } from 'react';
import { useFocusEffect } from '@react-navigation/native';
import { fetchUnreadConversationCount } from '../../api/messages';
import { subscribePmUnreadEpoch } from '../../notifications/pmUnreadEpoch';
import { useSession } from '../../session/SessionContext';

/**
 * Masthead-style unread conversation count. Fails soft (0) when signed out
 * or when the request fails, matching the website header.
 *
 * Refetches on focus and when a privateMessage receive bumps the PM epoch
 * while this hook is focused/mounted. Unfocused and unmounted screens skip
 * the network; the next focus/mount fetches.
 */
export function useUnreadConversationCount(): number {
  const { isSignedIn, accessToken } = useSession();
  const [count, setCount] = useState(0);

  useFocusEffect(
    useCallback(() => {
      if (!isSignedIn || accessToken == null) {
        setCount(0);
        return;
      }

      const token: string = accessToken;
      let controller: AbortController | undefined;

      function load(): void {
        controller?.abort();
        controller = new AbortController();
        fetchUnreadConversationCount(token, controller.signal)
          .then(setCount)
          .catch((err: unknown) => {
            if (err instanceof Error && err.name === 'AbortError') {
              return;
            }
            setCount(0);
          });
      }

      load();
      const unsubscribe = subscribePmUnreadEpoch(load);

      return () => {
        controller?.abort();
        unsubscribe();
      };
    }, [accessToken, isSignedIn]),
  );

  return count;
}
