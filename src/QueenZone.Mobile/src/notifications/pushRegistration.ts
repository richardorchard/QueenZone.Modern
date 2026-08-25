import AsyncStorage from '@react-native-async-storage/async-storage';
import { registerDevice, unregisterDevice } from './api';
import { getOrCreateDeviceId, peekDeviceId } from './deviceId';
import { checkNotificationPermission, ensureAndroidNotificationChannel, getDeviceToken, requestNotificationPermission } from './pushToken';

/** Last token this install successfully registered — lets a foreground/rotation check skip a no-op network call. */
const lastRegisteredTokenKey = 'queenzone.mobile.pushLastRegisteredToken';

/**
 * Ensures the signed-in member's device is registered for push (#850).
 * Best-effort throughout: a permission denial, an unavailable push service,
 * or a failed network call is swallowed so it never blocks sign-in or any
 * other app functionality.
 *
 * Safe to call repeatedly (sign-in, app foreground, token-rotation event) —
 * it only calls the register endpoint when the permission state or the
 * native token has actually changed since the last successful call.
 */
export async function syncPushRegistration(accessToken: string): Promise<void> {
  try {
    await ensureAndroidNotificationChannel();

    const granted = await requestNotificationPermission();
    if (!granted) {
      await clearPushRegistration(accessToken);
      return;
    }

    const result = await getDeviceToken();
    if (!result) {
      return;
    }

    const lastToken = await AsyncStorage.getItem(lastRegisteredTokenKey);
    if (lastToken === result.token) {
      return;
    }

    const deviceId = await getOrCreateDeviceId();
    await registerDevice(accessToken, deviceId, result.platform, result.token);
    await AsyncStorage.setItem(lastRegisteredTokenKey, result.token);
  } catch {
    // Registration is best-effort — never block the app on push setup.
  }
}

/**
 * Re-checks permission without prompting — used on app foreground so a
 * permission revoked from OS Settings unregisters the device without
 * surprising the member with a fresh prompt.
 */
export async function refreshPushRegistration(accessToken: string): Promise<void> {
  try {
    const granted = await checkNotificationPermission();
    if (!granted) {
      await clearPushRegistration(accessToken);
      return;
    }

    await syncPushRegistration(accessToken);
  } catch {
    // Best-effort.
  }
}

/** Unregisters this device (sign-out, or permission revoked after a previous register). */
export async function clearPushRegistration(accessToken: string): Promise<void> {
  try {
    const deviceId = await peekDeviceId();
    const hadToken = await AsyncStorage.getItem(lastRegisteredTokenKey);
    if (!deviceId || !hadToken) {
      return;
    }

    await AsyncStorage.removeItem(lastRegisteredTokenKey);
    await unregisterDevice(accessToken, deviceId);
  } catch {
    // Best-effort — a stale row is cleaned up server-side on next delivery attempt (#760).
  }
}
