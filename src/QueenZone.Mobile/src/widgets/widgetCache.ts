import AsyncStorage from '@react-native-async-storage/async-storage';
import type { OnThisDayAndroidWidgetProps } from './OnThisDayAndroidWidget';

const CACHE_KEY = 'widget:onThisDay:v1';
const REFRESH_AT_KEY = 'widget:onThisDay:refreshAt:v1';

export async function writeCachedWidgetProps(props: OnThisDayAndroidWidgetProps): Promise<void> {
  await AsyncStorage.setItem(CACHE_KEY, JSON.stringify(props));
}

/** Read the last props synced from the app. Returns `{}` (placeholder content) if none yet. */
export async function readCachedWidgetProps(): Promise<OnThisDayAndroidWidgetProps> {
  const raw = await AsyncStorage.getItem(CACHE_KEY);
  if (!raw) {
    return {};
  }
  try {
    return JSON.parse(raw) as OnThisDayAndroidWidgetProps;
  } catch {
    return {};
  }
}

export async function writeLastWidgetRefreshAt(at: number): Promise<void> {
  await AsyncStorage.setItem(REFRESH_AT_KEY, String(at));
}

/** Epoch ms of the last successful widget push, or `null` when none has been recorded. */
export async function readLastWidgetRefreshAt(): Promise<number | null> {
  const raw = await AsyncStorage.getItem(REFRESH_AT_KEY);
  if (!raw) {
    return null;
  }
  const value = Number(raw);
  return Number.isFinite(value) ? value : null;
}
