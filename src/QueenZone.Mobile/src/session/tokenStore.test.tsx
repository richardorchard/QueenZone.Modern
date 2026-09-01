import * as SecureStore from 'expo-secure-store';
import {
  clearStoredSession,
  isKeychainLockedError,
  KeychainLockedError,
  readStoredSession,
  writeStoredIdentityShell,
  writeStoredSession,
} from './tokenStore';

const mockMemory = new Map<string, string>();

const sessionStoreOptions = {
  keychainAccessible: SecureStore.AFTER_FIRST_UNLOCK,
};

jest.mock('expo-secure-store', () => ({
  AFTER_FIRST_UNLOCK: 'AFTER_FIRST_UNLOCK',
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
  (SecureStore.getItemAsync as jest.Mock).mockReset();
  (SecureStore.setItemAsync as jest.Mock).mockReset();
  (SecureStore.deleteItemAsync as jest.Mock).mockReset();
  (SecureStore.getItemAsync as jest.Mock).mockImplementation(async (key: string) => mockMemory.get(key) ?? null);
  (SecureStore.setItemAsync as jest.Mock).mockImplementation(async (key: string, value: string) => {
    mockMemory.set(key, value);
  });
  (SecureStore.deleteItemAsync as jest.Mock).mockImplementation(async (key: string) => {
    mockMemory.delete(key);
  });
});

function keychainLockedError(): Error {
  return Object.assign(new Error('User interaction is not allowed'), { name: 'KeyChainException' });
}

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
    expect(SecureStore.setItemAsync).toHaveBeenCalledWith(
      'queenzone.mobile.accessToken',
      'a',
      sessionStoreOptions,
    );
  });

  it('passes AFTER_FIRST_UNLOCK on every get, set, and delete', async () => {
    await writeStoredSession({ accessToken: 'a', refreshToken: 'r', expiresIn: 900 });
    await writeStoredIdentityShell({ displayName: 'Freddie', memberId: 'member-1' });
    await readStoredSession();
    await clearStoredSession();

    for (const call of (SecureStore.getItemAsync as jest.Mock).mock.calls) {
      expect(call[1]).toEqual(sessionStoreOptions);
    }
    for (const call of (SecureStore.setItemAsync as jest.Mock).mock.calls) {
      expect(call[2]).toEqual(sessionStoreOptions);
    }
    for (const call of (SecureStore.deleteItemAsync as jest.Mock).mock.calls) {
      expect(call[1]).toEqual(sessionStoreOptions);
    }
  });

  it('deletes each session item before setting it so accessibility can migrate', async () => {
    const order: string[] = [];
    (SecureStore.deleteItemAsync as jest.Mock).mockImplementation(async (key: string) => {
      order.push(`delete:${key}`);
      mockMemory.delete(key);
    });
    (SecureStore.setItemAsync as jest.Mock).mockImplementation(async (key: string, value: string) => {
      order.push(`set:${key}`);
      mockMemory.set(key, value);
    });

    await writeStoredSession({ accessToken: 'a', refreshToken: 'r', expiresIn: 900 });
    for (const key of [
      'queenzone.mobile.accessToken',
      'queenzone.mobile.refreshToken',
      'queenzone.mobile.accessExpiresAt',
    ]) {
      expect(order.filter((entry) => entry.endsWith(key))).toEqual([`delete:${key}`, `set:${key}`]);
    }

    order.length = 0;
    await writeStoredIdentityShell({ displayName: 'Freddie', memberId: 'member-1' });
    expect(order).toEqual([
      'delete:queenzone.mobile.identityShell',
      'set:queenzone.mobile.identityShell',
    ]);
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

  it('does not reject interaction-not-allowed as a raw Keychain throw', async () => {
    const raw = keychainLockedError();
    (SecureStore.getItemAsync as jest.Mock).mockRejectedValueOnce(raw);
    const rejected = readStoredSession();
    await expect(rejected).rejects.toBeInstanceOf(KeychainLockedError);
    await expect(rejected).rejects.toSatisfy(isKeychainLockedError);
    await expect(rejected).rejects.not.toBe(raw);
  });

  it('does not treat a locked keychain as a missing session', async () => {
    (SecureStore.getItemAsync as jest.Mock).mockRejectedValueOnce(keychainLockedError());
    await expect(readStoredSession()).rejects.toSatisfy((error: unknown) => {
      return isKeychainLockedError(error) && error != null;
    });
  });

  it('surfaces a locked write as isKeychainLockedError instead of a raw Keychain throw', async () => {
    const raw = keychainLockedError();
    (SecureStore.deleteItemAsync as jest.Mock).mockRejectedValueOnce(raw);
    await expect(
      writeStoredSession({ accessToken: 'a', refreshToken: 'r', expiresIn: 900 }),
    ).rejects.toBeInstanceOf(KeychainLockedError);
  });

  it('identifies the QUEENZONE-MOBILE-3 Keychain shape', () => {
    const sentryShape = new Error(
      'getValueWithKeyAsync failed with KeyChainException: User interaction is not allowed',
    );
    expect(isKeychainLockedError(sentryShape)).toBe(true);
    expect(isKeychainLockedError(keychainLockedError())).toBe(true);
    expect(isKeychainLockedError(new KeychainLockedError())).toBe(true);
    expect(isKeychainLockedError(new Error('secure-store unavailable'))).toBe(false);
    expect(isKeychainLockedError('interaction-not-allowed')).toBe(true);
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
      sessionStoreOptions,
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
