import AsyncStorage from '@react-native-async-storage/async-storage';
import { registerDevice, unregisterDevice } from './api';
import { getOrCreateDeviceId, peekDeviceId } from './deviceId';
import { resolvePushMemberId } from './pushMemberId';
import { checkNotificationPermission, ensureAndroidNotificationChannel, getDeviceToken, requestNotificationPermission } from './pushToken';

/** Legacy skip cache: native token only. Kept so upgrades re-register once under the current member. */
const lastRegisteredTokenKey = 'queenzone.mobile.pushLastRegisteredToken';
/** Last successful registration. Skip is token + member — token alone missed account switches (#1094). */
const lastRegistrationKey = 'queenzone.mobile.pushLastRegistration';

type LastPushRegistration = {
  token: string;
  memberId: string;
};

/**
 * Ensures the signed-in member's device is registered for push (#850).
 * Best-effort throughout: a permission denial, an unavailable push service,
 * or a failed network call is swallowed so it never blocks sign-in or any
 * other app functionality.
 *
 * Safe to call repeatedly (sign-in, app foreground, token-rotation event) —
 * it only calls the register endpoint when the permission state, the native
 * token, or the signed-in member has actually changed since the last
 * successful call.
 */
export async function syncPushRegistration(accessToken: string, memberId?: string | null): Promise<void> {
  try {
    await ensureAndroidNotificationChannel();

    const granted = await requestNotificationPermission();
    if (!granted) {
      await clearPushRegistration(accessToken, memberId);
      return;
    }

    const result = await getDeviceToken();
    if (!result) {
      return;
    }

    const ownerId = resolvePushMemberId(accessToken, memberId);
    const last = await readLastRegistration();
    if (ownerId && last && isSameRegistration(last, result.token, ownerId)) {
      return;
    }

    const deviceId = await getOrCreateDeviceId();
    await registerDevice(accessToken, deviceId, result.platform, result.token);
    await writeLastRegistration({ token: result.token, memberId: ownerId ?? '' });
  } catch {
    // Registration is best-effort — never block the app on push setup.
  }
}

/**
 * Re-checks permission without prompting — used on app foreground so a
 * permission revoked from OS Settings unregisters the device without
 * surprising the member with a fresh prompt.
 */
export async function refreshPushRegistration(accessToken: string, memberId?: string | null): Promise<void> {
  try {
    const granted = await checkNotificationPermission();
    if (!granted) {
      await clearPushRegistration(accessToken, memberId);
      return;
    }

    await syncPushRegistration(accessToken, memberId);
  } catch {
    // Best-effort.
  }
}

/** Unregisters this device (sign-out, or permission revoked after a previous register). */
export async function clearPushRegistration(accessToken: string, memberId?: string | null): Promise<void> {
  try {
    const deviceId = await peekDeviceId();
    const last = await readLastRegistration();
    if (!deviceId || !last) {
      return;
    }

    const ownerId = resolvePushMemberId(accessToken, memberId);
    // Re-read before dropping local skip state so a newer member's
    // registration (sign-in during fire-and-forget sign-out) is kept.
    if (shouldClearLocalRegistration(last, ownerId)) {
      const latest = await readLastRegistration();
      if (latest && shouldClearLocalRegistration(latest, ownerId)) {
        await clearLastRegistration();
      }
    }

    await unregisterDevice(accessToken, deviceId);
  } catch {
    // Best-effort — a stale row is cleaned up server-side on next delivery attempt (#760).
  }
}

function isSameRegistration(last: LastPushRegistration, token: string, memberId: string): boolean {
  return last.token === token && last.memberId.length > 0 && last.memberId === memberId;
}

function shouldClearLocalRegistration(last: LastPushRegistration, ownerId: string | null): boolean {
  return !ownerId || last.memberId.length === 0 || last.memberId === ownerId;
}

async function readLastRegistration(): Promise<LastPushRegistration | null> {
  const raw = await AsyncStorage.getItem(lastRegistrationKey);
  if (raw) {
    try {
      const parsed: unknown = JSON.parse(raw);
      if (
        parsed &&
        typeof parsed === 'object' &&
        typeof (parsed as LastPushRegistration).token === 'string' &&
        typeof (parsed as LastPushRegistration).memberId === 'string'
      ) {
        return { token: (parsed as LastPushRegistration).token, memberId: (parsed as LastPushRegistration).memberId };
      }
    } catch {
      // Fall through to the pre-#1094 token-only key.
    }
  }

  const legacyToken = await AsyncStorage.getItem(lastRegisteredTokenKey);
  if (!legacyToken) {
    return null;
  }

  return { token: legacyToken, memberId: '' };
}

async function writeLastRegistration(registration: LastPushRegistration): Promise<void> {
  await AsyncStorage.setItem(lastRegistrationKey, JSON.stringify(registration));
  await AsyncStorage.removeItem(lastRegisteredTokenKey);
}

async function clearLastRegistration(): Promise<void> {
  await AsyncStorage.multiRemove([lastRegistrationKey, lastRegisteredTokenKey]);
}
