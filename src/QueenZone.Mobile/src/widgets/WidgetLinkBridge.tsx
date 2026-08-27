import { useNavigation } from '@react-navigation/native';
import * as Linking from 'expo-linking';
import { useEffect, useRef } from 'react';
import { isWidgetTimelineUrl, openWidgetTimeline, type WidgetNavigation } from './widgetDeepLink';

/** Handles taps on the OS home screen widget — iOS `widgetURL` and Android `OPEN_URI`
 * both open `queenzone://timeline`, which lands here. */
export function WidgetLinkBridge() {
  const navigation = useNavigation<WidgetNavigation>();
  const navigationRef = useRef(navigation);
  navigationRef.current = navigation;

  useEffect(() => {
    function handleUrl(url: string) {
      if (isWidgetTimelineUrl(url)) {
        openWidgetTimeline(navigationRef.current);
      }
    }

    Linking.getInitialURL()
      .then((url) => {
        if (url) {
          handleUrl(url);
        }
      })
      .catch(() => {
        /* no initial URL to recover */
      });

    const subscription = Linking.addEventListener('url', ({ url }) => handleUrl(url));
    return () => subscription.remove();
  }, []);

  return null;
}
