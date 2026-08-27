import type { WidgetTaskHandlerProps } from 'react-native-android-widget';
import { OnThisDayAndroidWidget } from './OnThisDayAndroidWidget';
import { readCachedWidgetProps } from './widgetCache';
import { refreshHomeWidget } from './widgetSync';

async function renderCachedWidget(props: WidgetTaskHandlerProps): Promise<void> {
  const cached = await readCachedWidgetProps();
  props.renderWidget(<OnThisDayAndroidWidget {...cached} />);
}

/**
 * Android-only. Runs headless (system-triggered add/resize/periodic update, per
 * `updatePeriodMillis` in app.json). Periodic `WIDGET_UPDATE` fetches on-this-day
 * plus a new random quote; add/resize redraw from the last good cache.
 */
export async function widgetTaskHandler(props: WidgetTaskHandlerProps): Promise<void> {
  switch (props.widgetAction) {
    case 'WIDGET_UPDATE':
      await refreshHomeWidget();
      await renderCachedWidget(props);
      break;
    case 'WIDGET_ADDED':
    case 'WIDGET_RESIZED':
      await renderCachedWidget(props);
      break;
    case 'WIDGET_DELETED':
    default:
      break;
  }
}
