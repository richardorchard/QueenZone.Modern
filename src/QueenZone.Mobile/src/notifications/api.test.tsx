import { sendJson } from '../api/client';
import { registerDevice, unregisterDevice } from './api';

jest.mock('../api/client', () => ({
  sendJson: jest.fn(),
}));

const sendJsonMock = sendJson as jest.MockedFunction<typeof sendJson>;

describe('notifications api', () => {
  beforeEach(() => {
    sendJsonMock.mockReset();
  });

  it('registers a device', async () => {
    sendJsonMock.mockResolvedValue({ deviceId: 'device-1', platform: 'apns', updatedAt: '2026-01-01T00:00:00Z' });

    await registerDevice('token', 'device-1', 'apns', 'push-token');

    expect(sendJsonMock).toHaveBeenCalledWith('/notifications/devices', {
      method: 'POST',
      body: { deviceId: 'device-1', platform: 'apns', token: 'push-token' },
      accessToken: 'token',
    });
  });

  it('unregisters a device, encoding the deviceId', async () => {
    sendJsonMock.mockResolvedValue(undefined);

    await unregisterDevice('token', 'device/with slash');

    expect(sendJsonMock).toHaveBeenCalledWith('/notifications/devices/device%2Fwith%20slash', {
      method: 'DELETE',
      accessToken: 'token',
    });
  });
});
