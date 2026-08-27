import AsyncStorage from '@react-native-async-storage/async-storage';
import type { OnThisDayAndroidWidgetProps } from './OnThisDayAndroidWidget';

const CACHE_KEY = 'widget:onThisDay:v1';

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
