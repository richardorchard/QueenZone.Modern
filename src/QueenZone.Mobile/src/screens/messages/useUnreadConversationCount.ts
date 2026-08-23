import { useCallback, useState } from 'react';
import { useFocusEffect } from '@react-navigation/native';
import { fetchUnreadConversationCount } from '../../api/messages';
import { useSession } from '../../session/SessionContext';

/**
 * Masthead-style unread conversation count. Fails soft (0) when signed out
 * or when the request fails, matching the website header.
 */
export function useUnreadConversationCount(): number {
  const { isSignedIn, accessToken } = useSession();
  const [count, setCount] = useState(0);

  useFocusEffect(
    useCallback(() => {
      if (!isSignedIn || !accessToken) {
        setCount(0);
        return;
      }

      const controller = new AbortController();
      fetchUnreadConversationCount(accessToken, controller.signal)
        .then(setCount)
        .catch((err: unknown) => {
          if (err instanceof Error && err.name === 'AbortError') {
            return;
          }
          setCount(0);
        });

      return () => controller.abort();
    }, [accessToken, isSignedIn]),
  );

  return count;
}
