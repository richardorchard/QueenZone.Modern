import * as BackgroundTask from 'expo-background-task';
import * as TaskManager from 'expo-task-manager';
import { Platform } from 'react-native';
import { fetchOnThisDay, fetchRandomQuote } from '../api/content';
import type { RandomQuote, TimelineEvent } from '../api/types';
import type { OnThisDayAndroidWidgetProps } from './OnThisDayAndroidWidget';
import { readLastWidgetRefreshAt, writeCachedWidgetProps, writeLastWidgetRefreshAt } from './widgetCache';

export const HOME_WIDGET_BACKGROUND_TASK = 'queenzone-home-widget-refresh';

/** Android `updatePeriodMillis` and the iOS timeline hold. Hours, not real-time. */
export const WIDGET_REFRESH_INTERVAL_MS = 4 * 60 * 60 * 1000;

/** Skip a background fetch when the last successful push was this recent. */
export const WIDGET_REFRESH_MIN_INTERVAL_MS = 3 * 60 * 60 * 1000;

/** iOS BGTaskScheduler minimum interval, in minutes. Matches the 4-hour Android period. */
export const WIDGET_BACKGROUND_MIN_INTERVAL_MINUTES = 240;

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

function iosTimelineEntries(props: OnThisDayAndroidWidgetProps) {
  const now = Date.now();
  return [
    { date: new Date(now), props },
    { date: new Date(now + WIDGET_REFRESH_INTERVAL_MS), props },
  ];
}

function hasWidgetContent(content: WidgetContent): boolean {
  return content.onThisDay != null || content.quote != null;
}

/**
 * Pushes on-this-day + quote content to the OS widget. HomeScreen calls this after its
 * own fetches settle. Background refresh uses {@link refreshHomeWidget} instead.
 * The throttle timestamp is written only when the payload has content — a Home error
 * view (`{ onThisDay: null, quote: null }`) must not skip the next overnight fetch.
 */
export async function syncHomeWidget(content: WidgetContent): Promise<void> {
  const props = toWidgetProps(content);

  if (Platform.OS === 'ios') {
    // eslint-disable-next-line @typescript-eslint/no-require-imports -- platform-gated: this module must not load on Android, where expo-widgets has no native counterpart.
    const { OnThisDayWidget } = require('./OnThisDayWidget.ios') as typeof import('./OnThisDayWidget.ios');
    OnThisDayWidget.updateTimeline(iosTimelineEntries(props));
    if (hasWidgetContent(content)) {
      await writeLastWidgetRefreshAt(Date.now());
    }
    return;
  }

  if (Platform.OS === 'android') {
    await writeCachedWidgetProps(props);
    // eslint-disable-next-line @typescript-eslint/no-require-imports -- lazy so iOS bundles never touch the Android-only widget module.
    const { requestWidgetUpdate } = require('react-native-android-widget') as typeof import('react-native-android-widget');
    // eslint-disable-next-line @typescript-eslint/no-require-imports
    const { OnThisDayAndroidWidget } = require('./OnThisDayAndroidWidget') as typeof import('./OnThisDayAndroidWidget');
    await requestWidgetUpdate({
      widgetName: 'OnThisDayWidget',
      renderWidget: () => <OnThisDayAndroidWidget {...props} />,
    });
    if (hasWidgetContent(content)) {
      await writeLastWidgetRefreshAt(Date.now());
    }
  }
}

async function shouldSkipBackgroundRefresh(): Promise<boolean> {
  const last = await readLastWidgetRefreshAt();
  if (last == null) {
    return false;
  }
  return Date.now() - last < WIDGET_REFRESH_MIN_INTERVAL_MS;
}

/**
 * Fetches on-this-day plus a new random quote and pushes them to the widget.
 * Returns false when the fetch or push fails so the last good snapshot stays up.
 */
export async function refreshHomeWidget(): Promise<boolean> {
  if (await shouldSkipBackgroundRefresh()) {
    return true;
  }

  try {
    const [onThisDay, quote] = await Promise.all([fetchOnThisDay(), fetchRandomQuote()]);
    await syncHomeWidget({ onThisDay, quote });
    return true;
  } catch {
    return false;
  }
}

export async function runHomeWidgetBackgroundRefresh(): Promise<BackgroundTask.BackgroundTaskResult> {
  const ok = await refreshHomeWidget();
  return ok ? BackgroundTask.BackgroundTaskResult.Success : BackgroundTask.BackgroundTaskResult.Failed;
}

/** Must run from the JS entry so a background launch can find the task. */
export function defineHomeWidgetBackgroundTask(): void {
  TaskManager.defineTask(HOME_WIDGET_BACKGROUND_TASK, () => runHomeWidgetBackgroundRefresh());
}

/** iOS-only. Android already refreshes from the widget task's 4-hour period. */
export async function registerHomeWidgetBackgroundRefresh(): Promise<void> {
  if (Platform.OS !== 'ios') {
    return;
  }

  try {
    const registered = await TaskManager.isTaskRegisteredAsync(HOME_WIDGET_BACKGROUND_TASK);
    if (registered) {
      return;
    }
    await BackgroundTask.registerTaskAsync(HOME_WIDGET_BACKGROUND_TASK, {
      minimumInterval: WIDGET_BACKGROUND_MIN_INTERVAL_MINUTES,
    });
  } catch {
    /* Simulator / restricted OS — last snapshot stays until a run is granted. */
  }
}
