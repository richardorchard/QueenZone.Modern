import AsyncStorage from '@react-native-async-storage/async-storage';
import * as Crypto from 'expo-crypto';
import { getOrCreateDeviceId, peekDeviceId } from './deviceId';

jest.mock('expo-crypto', () => ({
  getRandomBytesAsync: jest.fn(async (n: number) => Uint8Array.from({ length: n }, (_, i) => i + 1)),
}));

describe('deviceId', () => {
  beforeEach(() => {
    (Crypto.getRandomBytesAsync as jest.Mock).mockClear();
  });

  afterEach(async () => {
    await AsyncStorage.clear();
  });

  it('creates and persists a device id on first use', async () => {
    expect(await peekDeviceId()).toBeNull();

    const id = await getOrCreateDeviceId();
    expect(id).toMatch(/^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/);
    expect(await peekDeviceId()).toBe(id);
  });

  it('reuses the stored device id on subsequent calls', async () => {
    const first = await getOrCreateDeviceId();
    const second = await getOrCreateDeviceId();
    expect(second).toBe(first);
    expect(Crypto.getRandomBytesAsync).toHaveBeenCalledTimes(1);
  });
});
