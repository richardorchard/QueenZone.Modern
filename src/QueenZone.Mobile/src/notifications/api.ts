import { sendJson } from '../api/client';

/** Matches `DevicePushPlatform` in QueenZone.Data (#757) — serialized lowercase. */
export type DevicePushPlatform = 'apns' | 'fcm';

export type DeviceRegisteredResponse = {
  deviceId: string;
  platform: DevicePushPlatform;
  updatedAt: string;
};

const devicesPath = '/notifications/devices';

export function registerDevice(
  accessToken: string,
  deviceId: string,
  platform: DevicePushPlatform,
  token: string,
): Promise<DeviceRegisteredResponse> {
  return sendJson(devicesPath, {
    method: 'POST',
    body: { deviceId, platform, token },
    accessToken,
  });
}

export function unregisterDevice(accessToken: string, deviceId: string): Promise<void> {
  return sendJson(`${devicesPath}/${encodeURIComponent(deviceId)}`, {
    method: 'DELETE',
    accessToken,
  });
}
