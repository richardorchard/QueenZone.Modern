import AsyncStorage from '@react-native-async-storage/async-storage';
import { registerDevice, unregisterDevice } from './api';
import { getOrCreateDeviceId, peekDeviceId } from './deviceId';
import { checkNotificationPermission, ensureAndroidNotificationChannel, getDeviceToken, requestNotificationPermission } from './pushToken';
import { clearPushRegistration, refreshPushRegistration, syncPushRegistration } from './pushRegistration';

jest.mock('./api', () => ({
  registerDevice: jest.fn(),
  unregisterDevice: jest.fn(),
}));

jest.mock('./deviceId', () => ({
  getOrCreateDeviceId: jest.fn(),
  peekDeviceId: jest.fn(),
}));

jest.mock('./pushToken', () => ({
  ensureAndroidNotificationChannel: jest.fn(async () => {}),
  requestNotificationPermission: jest.fn(),
  checkNotificationPermission: jest.fn(),
  getDeviceToken: jest.fn(),
}));

const registerDeviceMock = registerDevice as jest.MockedFunction<typeof registerDevice>;
const unregisterDeviceMock = unregisterDevice as jest.MockedFunction<typeof unregisterDevice>;
const getOrCreateDeviceIdMock = getOrCreateDeviceId as jest.MockedFunction<typeof getOrCreateDeviceId>;
const peekDeviceIdMock = peekDeviceId as jest.MockedFunction<typeof peekDeviceId>;
const requestNotificationPermissionMock = requestNotificationPermission as jest.MockedFunction<
  typeof requestNotificationPermission
>;
const checkNotificationPermissionMock = checkNotificationPermission as jest.MockedFunction<
  typeof checkNotificationPermission
>;
const getDeviceTokenMock = getDeviceToken as jest.MockedFunction<typeof getDeviceToken>;
const ensureAndroidNotificationChannelMock = ensureAndroidNotificationChannel as jest.MockedFunction<
  typeof ensureAndroidNotificationChannel
>;

describe('pushRegistration', () => {
  beforeEach(async () => {
    await AsyncStorage.clear();
    registerDeviceMock.mockReset().mockResolvedValue({ deviceId: 'device-1', platform: 'apns', updatedAt: '' });
    unregisterDeviceMock.mockReset().mockResolvedValue(undefined);
    getOrCreateDeviceIdMock.mockReset().mockResolvedValue('device-1');
    peekDeviceIdMock.mockReset().mockResolvedValue('device-1');
    requestNotificationPermissionMock.mockReset().mockResolvedValue(true);
    checkNotificationPermissionMock.mockReset().mockResolvedValue(true);
    getDeviceTokenMock.mockReset().mockResolvedValue({ platform: 'apns', token: 'native-token' });
    ensureAndroidNotificationChannelMock.mockClear();
  });

  describe('syncPushRegistration', () => {
    it('registers the device when permission is granted', async () => {
      await syncPushRegistration('access-token');

      expect(ensureAndroidNotificationChannelMock).toHaveBeenCalled();
      expect(registerDeviceMock).toHaveBeenCalledWith('access-token', 'device-1', 'apns', 'native-token');
    });

    it('does not call the register endpoint again for an unchanged token', async () => {
      await syncPushRegistration('access-token');
      await syncPushRegistration('access-token');

      expect(registerDeviceMock).toHaveBeenCalledTimes(1);
    });

    it('re-registers when the native token rotates', async () => {
      await syncPushRegistration('access-token');
      getDeviceTokenMock.mockResolvedValue({ platform: 'apns', token: 'rotated-token' });

      await syncPushRegistration('access-token');

      expect(registerDeviceMock).toHaveBeenCalledTimes(2);
      expect(registerDeviceMock).toHaveBeenLastCalledWith('access-token', 'device-1', 'apns', 'rotated-token');
    });

    it('does not register and does not block when permission is denied', async () => {
      requestNotificationPermissionMock.mockResolvedValue(false);

      await expect(syncPushRegistration('access-token')).resolves.toBeUndefined();

      expect(registerDeviceMock).not.toHaveBeenCalled();
    });

    it('swallows registration errors', async () => {
      registerDeviceMock.mockRejectedValue(new Error('network down'));

      await expect(syncPushRegistration('access-token')).resolves.toBeUndefined();
    });
  });

  describe('refreshPushRegistration', () => {
    it('unregisters when permission has been revoked since last registration', async () => {
      await syncPushRegistration('access-token');
      checkNotificationPermissionMock.mockResolvedValue(false);

      await refreshPushRegistration('access-token');

      expect(unregisterDeviceMock).toHaveBeenCalledWith('access-token', 'device-1');
    });

    it('re-syncs when permission is still granted', async () => {
      await refreshPushRegistration('access-token');
      expect(registerDeviceMock).toHaveBeenCalledWith('access-token', 'device-1', 'apns', 'native-token');
    });
  });

  describe('clearPushRegistration', () => {
    it('unregisters a previously registered device', async () => {
      await syncPushRegistration('access-token');

      await clearPushRegistration('access-token');

      expect(unregisterDeviceMock).toHaveBeenCalledWith('access-token', 'device-1');
    });

    it('does nothing when the device was never registered', async () => {
      await clearPushRegistration('access-token');
      expect(unregisterDeviceMock).not.toHaveBeenCalled();
    });

    it('swallows unregister errors', async () => {
      await syncPushRegistration('access-token');
      unregisterDeviceMock.mockRejectedValue(new Error('network down'));

      await expect(clearPushRegistration('access-token')).resolves.toBeUndefined();
    });
  });
});
