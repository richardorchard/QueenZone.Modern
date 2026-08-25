import * as Notifications from 'expo-notifications';
import { Platform } from 'react-native';
import type { DevicePushPlatform } from './api';

export type DeviceTokenResult = {
  platform: DevicePushPlatform;
  token: string;
};

/** Android 8+ requires a channel before a notification can display. */
export async function ensureAndroidNotificationChannel(): Promise<void> {
  if (Platform.OS !== 'android') {
    return;
  }

  await Notifications.setNotificationChannelAsync('default', {
    name: 'Default',
    importance: Notifications.AndroidImportance.DEFAULT,
  });
}

/**
 * True if the app can already send notifications, or the OS granted the
 * request. iOS will not re-prompt once explicitly denied — the member has
 * to change it in Settings, so a repeat request there resolves `false`
 * without a dialog rather than nagging.
 */
export async function requestNotificationPermission(): Promise<boolean> {
  if (Platform.OS !== 'ios' && Platform.OS !== 'android') {
    return false;
  }

  const current = await Notifications.getPermissionsAsync();
  if (current.granted) {
    return true;
  }

  const requested = await Notifications.requestPermissionsAsync({
    ios: { allowAlert: true, allowBadge: true, allowSound: true },
  });
  return requested.granted;
}

export async function checkNotificationPermission(): Promise<boolean> {
  if (Platform.OS !== 'ios' && Platform.OS !== 'android') {
    return false;
  }

  const status = await Notifications.getPermissionsAsync();
  return status.granted;
}

/** Native APNs/FCM token (never Expo's hosted push token — ADR 0014 rules out EAS). */
export async function getDeviceToken(): Promise<DeviceTokenResult | null> {
  const native = await Notifications.getDevicePushTokenAsync();
  if (native.type === 'ios') {
    return { platform: 'apns', token: native.data };
  }
  if (native.type === 'android') {
    return { platform: 'fcm', token: native.data };
  }
  return null;
}
