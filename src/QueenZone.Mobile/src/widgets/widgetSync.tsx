import { Platform } from 'react-native';
import type { RandomQuote, TimelineEvent } from '../api/types';
import type { OnThisDayAndroidWidgetProps } from './OnThisDayAndroidWidget';
import { writeCachedWidgetProps } from './widgetCache';

export type WidgetContent = {
  onThisDay: TimelineEvent | null;
  quote: RandomQuote | null;
};

function toWidgetProps(content: WidgetContent): OnThisDayAndroidWidgetProps {
  return {
    formattedDate: content.onThisDay?.formattedDate,
    summary: content.onThisDay?.summary,
    quoteText: content.quote?.text,
    quoteWhoSaid: content.quote?.whoSaid,
  };
}

/**
 * Pushes the home screen's on-this-day + quote content to the OS widget. Called from
 * HomeScreen once both fetches settle — this is the widget's only refresh trigger for now
 * (see widgetTaskHandler.tsx; a true background-fetch cadence is a follow-up, not #983's
 * scope).
 */
export async function syncHomeWidget(content: WidgetContent): Promise<void> {
  const props = toWidgetProps(content);

  if (Platform.OS === 'ios') {
    // eslint-disable-next-line @typescript-eslint/no-require-imports -- platform-gated: this module must not load on Android, where expo-widgets has no native counterpart.
    const { OnThisDayWidget } = require('./OnThisDayWidget.ios') as typeof import('./OnThisDayWidget.ios');
    OnThisDayWidget.updateSnapshot(props);
    return;
  }

  if (Platform.OS === 'android') {
    await writeCachedWidgetProps(props);
    // eslint-disable-next-line @typescript-eslint/no-require-imports -- lazy so iOS bundles never touch the Android-only widget module.
    const { requestWidgetUpdate } = require('react-native-android-widget') as typeof import('react-native-android-widget');
    // eslint-disable-next-line @typescript-eslint/no-require-imports
    const { OnThisDayAndroidWidget } = require('./OnThisDayAndroidWidget') as typeof import('./OnThisDayAndroidWidget');
    requestWidgetUpdate({
      widgetName: 'OnThisDayWidget',
      renderWidget: () => <OnThisDayAndroidWidget {...props} />,
    });
  }
}
