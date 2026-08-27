import { useNavigation } from '@react-navigation/native';
import * as Linking from 'expo-linking';
import { useEffect, useRef } from 'react';
import {
  consumeInitialWidgetUrl,
  isWidgetDeepLinkUrl,
  openWidgetDestination,
  type WidgetNavigation,
} from './widgetDeepLink';

/** Handles taps on the OS home screen widget — iOS `widgetURL` and Android `OPEN_URI`
 * open Home, where the on-this-day event and/or quote actually render. */
export function WidgetLinkBridge() {
  const navigation = useNavigation<WidgetNavigation>();
  const navigationRef = useRef(navigation);
  navigationRef.current = navigation;

  useEffect(() => {
    function handleUrl(url: string) {
      if (isWidgetDeepLinkUrl(url)) {
        openWidgetDestination(navigationRef.current);
      }
    }

    Linking.getInitialURL()
      .then((url) => {
        const widgetUrl = consumeInitialWidgetUrl(url);
        if (widgetUrl) {
          handleUrl(widgetUrl);
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
