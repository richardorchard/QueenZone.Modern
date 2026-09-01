import * as SecureStore from 'expo-secure-store';
import {
  clearStoredSession,
  readStoredSession,
  writeStoredIdentityShell,
  writeStoredSession,
} from './tokenStore';

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

  it('writes and reads a non-secret identity shell next to the grant', async () => {
    await writeStoredSession({ accessToken: 'a', refreshToken: 'r', expiresIn: 900 });
    await writeStoredIdentityShell({
      displayName: 'Freddie',
      memberId: 'member-1',
      avatarPath: '/avatars/1.jpg',
    });

    const stored = await readStoredSession();
    expect(stored?.refreshToken).toBe('r');
    expect(stored?.identity).toEqual({
      displayName: 'Freddie',
      memberId: 'member-1',
      avatarPath: '/avatars/1.jpg',
    });
    expect(SecureStore.setItemAsync).toHaveBeenCalledWith(
      'queenzone.mobile.identityShell',
      JSON.stringify({
        displayName: 'Freddie',
        memberId: 'member-1',
        avatarPath: '/avatars/1.jpg',
      }),
    );
    const persisted = mockMemory.get('queenzone.mobile.identityShell');
    expect(persisted).toBeTruthy();
    expect(persisted).not.toContain('email');
    expect(persisted).not.toContain('@');
  });

  it('clears the identity shell with the grant', async () => {
    await writeStoredSession({ accessToken: 'a', refreshToken: 'r', expiresIn: 900 });
    await writeStoredIdentityShell({ displayName: 'Freddie', memberId: 'member-1' });
    await clearStoredSession();
    await expect(readStoredSession()).resolves.toBeNull();
    await expect(SecureStore.getItemAsync('queenzone.mobile.identityShell')).resolves.toBeNull();
  });

  it('ignores a malformed identity shell without dropping the grant', async () => {
    await writeStoredSession({ accessToken: 'a', refreshToken: 'r', expiresIn: 900 });
    await SecureStore.setItemAsync('queenzone.mobile.identityShell', '{not-json');
    const stored = await readStoredSession();
    expect(stored?.accessToken).toBe('a');
    expect(stored?.identity).toBeNull();
  });

  it('keeps the grant and identity shell after a simulated app version bump', async () => {
    await writeStoredSession({ accessToken: 'a', refreshToken: 'r', expiresIn: 900 });
    await writeStoredIdentityShell({ displayName: 'Freddie', memberId: 'member-1' });

    const previousVersion = '0.1.0';
    const nextVersion = '0.1.214';
    expect(previousVersion).not.toBe(nextVersion);
    expect(mockMemory.has('queenzone.mobile.accessToken')).toBe(true);
    expect(mockMemory.has(`queenzone.mobile.accessToken.${nextVersion}`)).toBe(false);
    expect(mockMemory.has(`queenzone.mobile.identityShell.${nextVersion}`)).toBe(false);

    const stored = await readStoredSession();
    expect(stored?.refreshToken).toBe('r');
    expect(stored?.identity?.displayName).toBe('Freddie');
    expect(stored?.identity?.memberId).toBe('member-1');
  });
});
