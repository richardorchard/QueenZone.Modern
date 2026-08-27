import type { WidgetTaskHandlerProps } from 'react-native-android-widget';
import { OnThisDayAndroidWidget } from './OnThisDayAndroidWidget';
import { readCachedWidgetProps } from './widgetCache';

/**
 * Android-only. Runs headless (system-triggered add/resize/periodic update, per
 * `updatePeriodMillis` in app.json) — it cannot read React state, so it re-renders from
 * whatever `syncHomeWidget` last cached, not a fresh API fetch (see #983 scope: refresh
 * cadence tied to the app being opened, not a background fetch job).
 */
export async function widgetTaskHandler(props: WidgetTaskHandlerProps): Promise<void> {
  switch (props.widgetAction) {
    case 'WIDGET_ADDED':
    case 'WIDGET_UPDATE':
    case 'WIDGET_RESIZED': {
      const cached = await readCachedWidgetProps();
      props.renderWidget(<OnThisDayAndroidWidget {...cached} />);
      break;
    }
    case 'WIDGET_DELETED':
    default:
      break;
  }
}
