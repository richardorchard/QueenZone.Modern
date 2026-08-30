import type { WidgetFace } from './widgetCopy';

/** Opened by the On This Day face and by a quote face that has no usable id. */
export const widgetDeepLinkUrl = 'queenzone://home';

const widgetHomeHosts = new Set(['home', 'timeline']);

function widgetHost(parsed: URL): string {
  return parsed.hostname || parsed.pathname.replace(/^\/+/, '').replace(/\/+$/, '');
}

function widgetPathSegment(parsed: URL): string {
  return parsed.pathname.replace(/^\/+/, '').replace(/\/+$/, '').split('/')[0] ?? '';
}

export function widgetQuoteDeepLinkUrl(id: number): string {
  return `queenzone://quotes/${id}`;
}

/** Face-specific tap URL. Quote face with a real id opens that quote; everything else is Home. */
export function widgetFaceDeepLinkUrl(face: WidgetFace | null, quoteId?: number): string {
  if (face === 'quote' && quoteId != null && quoteId > 0) {
    return widgetQuoteDeepLinkUrl(quoteId);
  }
  return widgetDeepLinkUrl;
}

export function parseWidgetQuoteId(url: string): number | null {
  try {
    const parsed = new URL(url);
    if (parsed.protocol !== 'queenzone:') {
      return null;
    }
    if (widgetHost(parsed) !== 'quotes') {
      return null;
    }
    const segment = widgetPathSegment(parsed);
    if (!/^\d+$/.test(segment)) {
      return null;
    }
    const id = Number(segment);
    return id > 0 ? id : null;
  } catch {
    return null;
  }
}

/** True for Home, the older Timeline URL, and quote-by-id (`queenzone://quotes/{id}`). */
export function isWidgetDeepLinkUrl(url: string): boolean {
  try {
    const parsed = new URL(url);
    if (parsed.protocol !== 'queenzone:') {
      return false;
    }
    const host = widgetHost(parsed);
    return widgetHomeHosts.has(host) || host === 'quotes';
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

export type WidgetHomeDestination = {
  screen: 'HomeTab';
  params: { screen: 'Home'; initial: false };
};

export type WidgetQuoteDestination = {
  screen: 'HomeTab';
  params: { screen: 'Quote'; params: { id: number }; initial: false };
};

export type WidgetNavigation = {
  navigate: (name: 'Tabs', params: WidgetHomeDestination | WidgetQuoteDestination) => void;
};

export function openWidgetDestination(navigation: WidgetNavigation, url?: string): void {
  const quoteId = url ? parseWidgetQuoteId(url) : null;
  if (quoteId != null) {
    navigation.navigate('Tabs', {
      screen: 'HomeTab',
      params: { screen: 'Quote', params: { id: quoteId }, initial: false },
    });
    return;
  }

  navigation.navigate('Tabs', {
    screen: 'HomeTab',
    params: { screen: 'Home', initial: false },
  });
}
