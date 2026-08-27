/** Opened by both the iOS widget's `widgetURL` and the Android widget's `OPEN_URI` click action. */
export const widgetDeepLinkUrl = 'queenzone://timeline';

export function isWidgetTimelineUrl(url: string): boolean {
  try {
    const parsed = new URL(url);
    if (parsed.protocol !== 'queenzone:') {
      return false;
    }
    return parsed.hostname === 'timeline' || parsed.pathname.replace(/^\/+/, '') === 'timeline';
  } catch {
    return false;
  }
}

export type WidgetNavigation = {
  navigate: (
    name: 'Tabs',
    params: {
      screen: 'ArchiveTab';
      params: { screen: 'Timeline'; params: object; initial: false };
    },
  ) => void;
};

export function openWidgetTimeline(navigation: WidgetNavigation): void {
  navigation.navigate('Tabs', {
    screen: 'ArchiveTab',
    params: { screen: 'Timeline', params: {}, initial: false },
  });
}
