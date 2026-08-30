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

const memberA = '11111111-1111-1111-1111-111111111111';
const memberB = '22222222-2222-2222-2222-222222222222';

function accessJwt(memberId: string): string {
  const encode = (value: object) => Buffer.from(JSON.stringify(value)).toString('base64url');
  return `${encode({ alg: 'none', typ: 'JWT' })}.${encode({ sub: memberId })}.sig`;
}

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
      await syncPushRegistration('access-token', memberA);

      expect(ensureAndroidNotificationChannelMock).toHaveBeenCalled();
      expect(registerDeviceMock).toHaveBeenCalledWith('access-token', 'device-1', 'apns', 'native-token');
    });

    it('does not call the register endpoint again for an unchanged token of the same member', async () => {
      await syncPushRegistration('access-token', memberA);
      await syncPushRegistration('access-token', memberA);

      expect(registerDeviceMock).toHaveBeenCalledTimes(1);
    });

    it('does not re-register when the access token refreshes for the same member', async () => {
      await syncPushRegistration(accessJwt(memberA));
      await syncPushRegistration(accessJwt(memberA));

      expect(registerDeviceMock).toHaveBeenCalledTimes(1);
    });

    it('re-registers when the native token rotates', async () => {
      await syncPushRegistration('access-token', memberA);
      getDeviceTokenMock.mockResolvedValue({ platform: 'apns', token: 'rotated-token' });

      await syncPushRegistration('access-token', memberA);

      expect(registerDeviceMock).toHaveBeenCalledTimes(2);
      expect(registerDeviceMock).toHaveBeenLastCalledWith('access-token', 'device-1', 'apns', 'rotated-token');
    });

    it('re-registers when a different member signs in on the same device token (#1094)', async () => {
      await syncPushRegistration('token-a', memberA);
      await syncPushRegistration('token-b', memberB);

      expect(registerDeviceMock).toHaveBeenCalledTimes(2);
      expect(registerDeviceMock).toHaveBeenLastCalledWith('token-b', 'device-1', 'apns', 'native-token');
    });

    it('re-registers from JWT sub when the signed-in member changes without an explicit id', async () => {
      await syncPushRegistration(accessJwt(memberA));
      await syncPushRegistration(accessJwt(memberB));

      expect(registerDeviceMock).toHaveBeenCalledTimes(2);
      expect(registerDeviceMock).toHaveBeenLastCalledWith(accessJwt(memberB), 'device-1', 'apns', 'native-token');
    });

    it('re-registers a legacy token-only cache under the current member', async () => {
      await AsyncStorage.setItem('queenzone.mobile.pushLastRegisteredToken', 'native-token');

      await syncPushRegistration('access-token', memberA);

      expect(registerDeviceMock).toHaveBeenCalledTimes(1);
    });

    it('does not register and does not block when permission is denied', async () => {
      requestNotificationPermissionMock.mockResolvedValue(false);

      await expect(syncPushRegistration('access-token', memberA)).resolves.toBeUndefined();

      expect(registerDeviceMock).not.toHaveBeenCalled();
    });

    it('swallows registration errors', async () => {
      registerDeviceMock.mockRejectedValue(new Error('network down'));

      await expect(syncPushRegistration('access-token', memberA)).resolves.toBeUndefined();
    });
  });

  describe('refreshPushRegistration', () => {
    it('unregisters when permission has been revoked since last registration', async () => {
      await syncPushRegistration('access-token', memberA);
      checkNotificationPermissionMock.mockResolvedValue(false);

      await refreshPushRegistration('access-token', memberA);

      expect(unregisterDeviceMock).toHaveBeenCalledWith('access-token', 'device-1');
    });

    it('re-syncs when permission is still granted', async () => {
      await refreshPushRegistration('access-token', memberA);
      expect(registerDeviceMock).toHaveBeenCalledWith('access-token', 'device-1', 'apns', 'native-token');
    });
  });

  describe('clearPushRegistration', () => {
    it('unregisters a previously registered device', async () => {
      await syncPushRegistration('access-token', memberA);

      await clearPushRegistration('access-token', memberA);

      expect(unregisterDeviceMock).toHaveBeenCalledWith('access-token', 'device-1');
    });

    it('does nothing when the device was never registered', async () => {
      await clearPushRegistration('access-token', memberA);
      expect(unregisterDeviceMock).not.toHaveBeenCalled();
    });

    it('swallows unregister errors', async () => {
      await syncPushRegistration('access-token', memberA);
      unregisterDeviceMock.mockRejectedValue(new Error('network down'));

      await expect(clearPushRegistration('access-token', memberA)).resolves.toBeUndefined();
    });

    it('does not drop the new member skip state when the previous member unregisters late (#1094)', async () => {
      await syncPushRegistration('token-a', memberA);
      await syncPushRegistration('token-b', memberB);
      expect(registerDeviceMock).toHaveBeenCalledTimes(2);

      await clearPushRegistration('token-a', memberA);

      expect(unregisterDeviceMock).toHaveBeenCalledWith('token-a', 'device-1');
      registerDeviceMock.mockClear();
      await syncPushRegistration('token-b', memberB);
      expect(registerDeviceMock).not.toHaveBeenCalled();
    });

    it('still registers the new member when they sign in before the previous unregister finishes', async () => {
      await syncPushRegistration('token-a', memberA);

      const previousSignOut = clearPushRegistration('token-a', memberA);
      await syncPushRegistration('token-b', memberB);
      await previousSignOut;

      expect(registerDeviceMock).toHaveBeenCalledTimes(2);
      expect(registerDeviceMock).toHaveBeenLastCalledWith('token-b', 'device-1', 'apns', 'native-token');

      registerDeviceMock.mockClear();
      await syncPushRegistration('token-b', memberB);
      expect(registerDeviceMock).not.toHaveBeenCalled();
    });
  });
});
