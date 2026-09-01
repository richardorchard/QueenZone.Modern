import * as Notifications from 'expo-notifications';
import { AppState, Platform } from 'react-native';
import {
  checkNotificationPermission,
  ensureAndroidNotificationChannel,
  getDeviceToken,
  requestNotificationPermission,
  waitUntilAppActive,
} from './pushToken';

const getPermissionsAsync = Notifications.getPermissionsAsync as jest.MockedFunction<
  typeof Notifications.getPermissionsAsync
>;
const requestPermissionsAsync = Notifications.requestPermissionsAsync as jest.MockedFunction<
  typeof Notifications.requestPermissionsAsync
>;
const getDevicePushTokenAsync = Notifications.getDevicePushTokenAsync as jest.MockedFunction<
  typeof Notifications.getDevicePushTokenAsync
>;
const setNotificationChannelAsync = Notifications.setNotificationChannelAsync as jest.MockedFunction<
  typeof Notifications.setNotificationChannelAsync
>;

function permissionStatus(granted: boolean) {
  return { granted, canAskAgain: !granted, expires: 'never', status: granted ? 'granted' : 'undetermined' } as Awaited<
    ReturnType<typeof Notifications.getPermissionsAsync>
  >;
}

describe('pushToken', () => {
  beforeEach(() => {
    Object.defineProperty(AppState, 'currentState', { value: 'active', configurable: true });
  });

  afterEach(() => {
    Object.defineProperty(Platform, 'OS', { value: 'ios', configurable: true });
    Object.defineProperty(AppState, 'currentState', { value: 'active', configurable: true });
  });

  describe('requestNotificationPermission', () => {
    it('returns true without prompting when already granted', async () => {
      getPermissionsAsync.mockResolvedValue(permissionStatus(true));
      await expect(requestNotificationPermission()).resolves.toBe(true);
      expect(requestPermissionsAsync).not.toHaveBeenCalled();
    });

    it('prompts and returns the result when not yet granted', async () => {
      getPermissionsAsync.mockResolvedValue(permissionStatus(false));
      requestPermissionsAsync.mockResolvedValue(permissionStatus(true));
      await expect(requestNotificationPermission()).resolves.toBe(true);
      expect(requestPermissionsAsync).toHaveBeenCalledWith({
        ios: { allowAlert: true, allowBadge: true, allowSound: true },
      });
    });

    it('resolves false when the member declines', async () => {
      getPermissionsAsync.mockResolvedValue(permissionStatus(false));
      requestPermissionsAsync.mockResolvedValue(permissionStatus(false));
      await expect(requestNotificationPermission()).resolves.toBe(false);
    });

    it('resolves false on unsupported platforms without touching native APIs', async () => {
      Object.defineProperty(Platform, 'OS', { value: 'web', configurable: true });
      await expect(requestNotificationPermission()).resolves.toBe(false);
      expect(getPermissionsAsync).not.toHaveBeenCalled();
    });
  });

  describe('checkNotificationPermission', () => {
    it('reflects the current permission state', async () => {
      getPermissionsAsync.mockResolvedValue(permissionStatus(true));
      await expect(checkNotificationPermission()).resolves.toBe(true);
    });
  });

  describe('waitUntilAppActive', () => {
    it('does not subscribe when the app is already active', async () => {
      const addSpy = jest.spyOn(AppState, 'addEventListener');
      await waitUntilAppActive();
      expect(addSpy).not.toHaveBeenCalled();
      addSpy.mockRestore();
    });
  });

  describe('getDeviceToken', () => {
    it('waits until AppState is active before talking to APNs', async () => {
      Object.defineProperty(AppState, 'currentState', { value: 'background', configurable: true });
      let onChange: ((state: string) => void) | undefined;
      const remove = jest.fn();
      const addSpy = jest.spyOn(AppState, 'addEventListener').mockImplementation((_type, handler) => {
        onChange = handler as (state: string) => void;
        return { remove };
      });
      getDevicePushTokenAsync.mockResolvedValue({ type: 'ios', data: 'apns-token' });

      const pending = getDeviceToken();
      await Promise.resolve();
      expect(getDevicePushTokenAsync).not.toHaveBeenCalled();

      onChange?.('active');
      await expect(pending).resolves.toEqual({ platform: 'apns', token: 'apns-token' });
      expect(remove).toHaveBeenCalled();
      addSpy.mockRestore();
      Object.defineProperty(AppState, 'currentState', { value: 'active', configurable: true });
    });

    it('maps an iOS token to apns', async () => {

    it('maps an Android token to fcm', async () => {
      getDevicePushTokenAsync.mockResolvedValue({ type: 'android', data: 'fcm-token' });
      await expect(getDeviceToken()).resolves.toEqual({ platform: 'fcm', token: 'fcm-token' });
    });
  });

  describe('ensureAndroidNotificationChannel', () => {
    it('sets the default channel on Android', async () => {
      Object.defineProperty(Platform, 'OS', { value: 'android', configurable: true });
      await ensureAndroidNotificationChannel();
      expect(setNotificationChannelAsync).toHaveBeenCalledWith('default', expect.any(Object));
    });

    it('does nothing on iOS', async () => {
      await ensureAndroidNotificationChannel();
      expect(setNotificationChannelAsync).not.toHaveBeenCalled();
    });
  });
});
