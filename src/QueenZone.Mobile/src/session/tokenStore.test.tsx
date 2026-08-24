import * as SecureStore from 'expo-secure-store';
import { clearStoredSession, readStoredSession, writeStoredSession } from './tokenStore';

const mockMemory = new Map<string, string>();

jest.mock('expo-secure-store', () => ({
  getItemAsync: jest.fn(async (key: string) => mockMemory.get(key) ?? null),
  setItemAsync: jest.fn(async (key: string, value: string) => {
    mockMemory.set(key, value);
  }),
  deleteItemAsync: jest.fn(async (key: string) => {
    mockMemory.delete(key);
  }),
}));

beforeEach(() => {
  mockMemory.clear();
});

describe('tokenStore', () => {
  it('writes and reads a stored session', async () => {
    const stored = await writeStoredSession({
      accessToken: 'a',
      refreshToken: 'r',
      expiresIn: 900,
    });
    expect(stored.accessToken).toBe('a');
    expect(stored.refreshToken).toBe('r');
    expect(stored.expiresAt).toBeGreaterThan(Date.now());

    const roundTrip = await readStoredSession();
    expect(roundTrip?.accessToken).toBe('a');
    expect(roundTrip?.refreshToken).toBe('r');
    expect(roundTrip?.expiresAt).toBe(stored.expiresAt);
    expect(SecureStore.setItemAsync).toHaveBeenCalledWith('queenzone.mobile.accessToken', 'a');
  });

  it('returns null when either token is missing', async () => {
    await SecureStore.setItemAsync('queenzone.mobile.accessToken', 'a');
    await expect(readStoredSession()).resolves.toBeNull();
  });

  it('clears stored tokens', async () => {
    await writeStoredSession({ accessToken: 'a', refreshToken: 'r', expiresIn: 900 });
    await clearStoredSession();
    await expect(readStoredSession()).resolves.toBeNull();
  });

  it('surfaces SecureStore failures instead of swallowing them', async () => {
    (SecureStore.getItemAsync as jest.Mock).mockRejectedValueOnce(new Error('secure-store unavailable'));
    await expect(readStoredSession()).rejects.toThrow('secure-store unavailable');
  });
});
