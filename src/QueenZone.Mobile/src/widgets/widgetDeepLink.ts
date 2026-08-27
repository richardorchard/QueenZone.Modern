/** Opened by both the iOS widget's `widgetURL` and the Android widget's `OPEN_URI` click action. */
export const widgetDeepLinkUrl = 'queenzone://home';

const widgetHosts = new Set(['home', 'timeline']);

function widgetHost(parsed: URL): string {
  return parsed.hostname || parsed.pathname.replace(/^\/+/, '').replace(/\/+$/, '');
}

/** True for the current Home destination and the older Timeline URL already on devices. */
export function isWidgetDeepLinkUrl(url: string): boolean {
  try {
    const parsed = new URL(url);
    if (parsed.protocol !== 'queenzone:') {
      return false;
    }
    return widgetHosts.has(widgetHost(parsed));
  } catch {
    return false;
  }
}

let consumedInitialWidgetUrl = false;

/**
 * Cold-start widget URL is handled once per process so a RootNavigator remount
 * cannot re-apply it. Later `url` events (tap while the app is running) are
 * not gated here. Smoke-auth and other schemes are never consumed.
 */
export function consumeInitialWidgetUrl(url: string | null): string | null {
  if (!url || !isWidgetDeepLinkUrl(url)) {
    return null;
  }
  if (consumedInitialWidgetUrl) {
    return null;
  }
  consumedInitialWidgetUrl = true;
  return url;
}

export function resetInitialWidgetUrlConsumption(): void {
  consumedInitialWidgetUrl = false;
}

export type WidgetNavigation = {
  navigate: (
    name: 'Tabs',
    params: {
      screen: 'HomeTab';
      params: { screen: 'Home'; initial: false };
    },
  ) => void;
};

export function openWidgetDestination(navigation: WidgetNavigation): void {
  navigation.navigate('Tabs', {
    screen: 'HomeTab',
    params: { screen: 'Home', initial: false },
  });
}
