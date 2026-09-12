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
      'queenzone.mobile.grant',
      JSON.stringify({ accessToken: 'a', refreshToken: 'r', expiresAt: stored.expiresAt }),
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
    expect(order).toEqual(['delete:queenzone.mobile.grant', 'set:queenzone.mobile.grant']);

    order.length = 0;
    await writeStoredIdentityShell({ displayName: 'Freddie', memberId: 'member-1' });
    expect(order).toEqual([
      'delete:queenzone.mobile.identityShell',
      'set:queenzone.mobile.identityShell',
    ]);
  });

  it('returns null when there is no grant', async () => {
    await expect(readStoredSession()).resolves.toBeNull();
  });

  it('never leaves an access token without a refresh token when the write is interrupted mid-flight', async () => {
    await writeStoredSession({ accessToken: 'old-a', refreshToken: 'old-r', expiresIn: 900 });

    (SecureStore.setItemAsync as jest.Mock).mockImplementationOnce(async () => {
      throw new Error('process killed mid-write');
    });
    await expect(
      writeStoredSession({ accessToken: 'new-a', refreshToken: 'new-r', expiresIn: 900 }),
    ).rejects.toThrow('process killed mid-write');

    // The delete half of the torn write already ran, so the grant is gone entirely —
    // never left with a new access token and the old (or no) refresh token.
    await expect(readStoredSession()).resolves.toBeNull();
  });

  it('migrates a legacy per-field grant to the combined key without signing the member out', async () => {
    await SecureStore.setItemAsync('queenzone.mobile.accessToken', 'legacy-a');
    await SecureStore.setItemAsync('queenzone.mobile.refreshToken', 'legacy-r');
    await SecureStore.setItemAsync('queenzone.mobile.accessExpiresAt', '12345');

    const stored = await readStoredSession();
    expect(stored?.accessToken).toBe('legacy-a');
    expect(stored?.refreshToken).toBe('legacy-r');
    expect(stored?.expiresAt).toBe(12345);

    expect(mockMemory.has('queenzone.mobile.accessToken')).toBe(false);
    expect(mockMemory.has('queenzone.mobile.refreshToken')).toBe(false);
    expect(mockMemory.has('queenzone.mobile.accessExpiresAt')).toBe(false);
    expect(mockMemory.get('queenzone.mobile.grant')).toBe(
      JSON.stringify({ accessToken: 'legacy-a', refreshToken: 'legacy-r', expiresAt: 12345 }),
    );

    const again = await readStoredSession();
    expect(again?.accessToken).toBe('legacy-a');
  });

  it('does not migrate a legacy grant missing either field', async () => {
    await SecureStore.setItemAsync('queenzone.mobile.accessToken', 'legacy-a');
    await expect(readStoredSession()).resolves.toBeNull();
    expect(mockMemory.has('queenzone.mobile.grant')).toBe(false);
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
    await expect(rejected).rejects.not.toBe(raw);
    await expect(rejected).rejects.toMatchObject({ name: 'KeychainLockedError' });
  });

  it('does not treat a locked keychain as a missing session', async () => {
    (SecureStore.getItemAsync as jest.Mock).mockRejectedValueOnce(keychainLockedError());
    let thrown: unknown;
    try {
      await readStoredSession();
    } catch (error) {
      thrown = error;
    }
    expect(thrown).toBeDefined();
    expect(thrown).not.toBeNull();
    expect(isKeychainLockedError(thrown)).toBe(true);
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
    expect(mockMemory.has('queenzone.mobile.grant')).toBe(true);
    expect(mockMemory.has(`queenzone.mobile.grant.${nextVersion}`)).toBe(false);
    expect(mockMemory.has(`queenzone.mobile.identityShell.${nextVersion}`)).toBe(false);

    const stored = await readStoredSession();
    expect(stored?.refreshToken).toBe('r');
    expect(stored?.identity?.displayName).toBe('Freddie');
    expect(stored?.identity?.memberId).toBe('member-1');
  });
});
