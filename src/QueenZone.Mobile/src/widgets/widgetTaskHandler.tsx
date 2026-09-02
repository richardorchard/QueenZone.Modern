import type { WidgetTaskHandlerProps } from 'react-native-android-widget';
import { OnThisDayAndroidWidget } from './OnThisDayAndroidWidget';
import { readCachedWidgetProps } from './widgetCache';
import { refreshHomeWidget } from './widgetSync';

async function renderCachedWidget(props: WidgetTaskHandlerProps): Promise<void> {
  const cached = await readCachedWidgetProps();
  props.renderWidget(<OnThisDayAndroidWidget {...cached} widgetWidth={props.widgetInfo.width} />);
}

/**
 * Android-only. Runs headless (system-triggered add/resize/periodic update, per
 * `updatePeriodMillis` in app.json). Periodic `WIDGET_UPDATE` fetches a new random
 * quote and trivia (~4 hours) and on-this-day only when the calendar day changed;
 * add/resize redraw from the last good cache. A failed fetch keeps the previous value.
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
