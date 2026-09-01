import * as SecureStore from 'expo-secure-store';
import type { AuthTokens } from '../api/auth';

const accessKey = 'queenzone.mobile.accessToken';
const refreshKey = 'queenzone.mobile.refreshToken';
const expiryKey = 'queenzone.mobile.accessExpiresAt';
/** Sibling of the grant. Stable across store/TestFlight binaries — not version-namespaced. */
const identityKey = 'queenzone.mobile.identityShell';

/** Shared iOS accessibility for the four session keys. Do not add requireAuthentication. */
const sessionStoreOptions: SecureStore.SecureStoreOptions = {
  keychainAccessible: SecureStore.AFTER_FIRST_UNLOCK,
};

export type StoredIdentityShell = {
  displayName: string;
  memberId: string;
  avatarPath?: string | null;
};

export type StoredSession = AuthTokens & {
  expiresAt: number;
  identity?: StoredIdentityShell | null;
};

export class KeychainLockedError extends Error {
  constructor(cause?: unknown) {
    super('User interaction is not allowed', cause === undefined ? undefined : { cause });
    this.name = 'KeychainLockedError';
  }
}

function errorText(error: unknown): { name: string; message: string } {
  if (error instanceof Error) {
    return { name: error.name, message: error.message };
  }
  return { name: '', message: typeof error === 'string' ? error : '' };
}

/** Locked-device / background Keychain — not a missing session and not a generic outage. */
export function isKeychainLockedError(error: unknown): boolean {
  if (error instanceof KeychainLockedError) {
    return true;
  }
  const { name, message } = errorText(error);
  const haystack = `${name} ${message}`;
  return (
    name === 'KeyChainException' ||
    /KeyChainException/i.test(haystack) ||
    /user interaction is not allowed/i.test(haystack) ||
    /interaction[- ]not[- ]allowed/i.test(haystack)
  );
}

function rethrowKeychainError(error: unknown): never {
  if (isKeychainLockedError(error)) {
    throw error instanceof KeychainLockedError ? error : new KeychainLockedError(error);
  }
  throw error;
}

/** Delete then set — SecItemUpdate cannot change accessibility (expo/expo#23924). */
async function writeSessionItem(key: string, value: string): Promise<void> {
  await SecureStore.deleteItemAsync(key, sessionStoreOptions);
  await SecureStore.setItemAsync(key, value, sessionStoreOptions);
}

export async function readStoredSession(): Promise<StoredSession | null> {
  try {
    const [accessToken, refreshToken, expiry, identityRaw] = await Promise.all([
      SecureStore.getItemAsync(accessKey, sessionStoreOptions),
      SecureStore.getItemAsync(refreshKey, sessionStoreOptions),
      SecureStore.getItemAsync(expiryKey, sessionStoreOptions),
      SecureStore.getItemAsync(identityKey, sessionStoreOptions),
    ]);
    if (!accessToken || !refreshToken) {
      return null;
    }

    const expiresAt = expiry ? Number.parseInt(expiry, 10) : 0;
    return {
      accessToken,
      refreshToken,
      expiresIn: 900,
      expiresAt: Number.isFinite(expiresAt) ? expiresAt : 0,
      identity: parseIdentityShell(identityRaw),
    };
  } catch (error) {
    rethrowKeychainError(error);
  }
}

export async function writeStoredSession(tokens: AuthTokens): Promise<StoredSession> {
  const expiresAt = Date.now() + Math.max(tokens.expiresIn - 30, 30) * 1000;
  try {
    await Promise.all([
      writeSessionItem(accessKey, tokens.accessToken),
      writeSessionItem(refreshKey, tokens.refreshToken),
      writeSessionItem(expiryKey, String(expiresAt)),
    ]);
  } catch (error) {
    rethrowKeychainError(error);
  }
  return { ...tokens, expiresAt };
}

export async function writeStoredIdentityShell(shell: StoredIdentityShell): Promise<void> {
  const payload: StoredIdentityShell = {
    displayName: shell.displayName,
    memberId: shell.memberId,
  };
  if (shell.avatarPath) {
    payload.avatarPath = shell.avatarPath;
  }
  try {
    await writeSessionItem(identityKey, JSON.stringify(payload));
  } catch (error) {
    rethrowKeychainError(error);
  }
}

export async function clearStoredSession(): Promise<void> {
  await Promise.all([
    SecureStore.deleteItemAsync(accessKey, sessionStoreOptions),
    SecureStore.deleteItemAsync(refreshKey, sessionStoreOptions),
    SecureStore.deleteItemAsync(expiryKey, sessionStoreOptions),
    SecureStore.deleteItemAsync(identityKey, sessionStoreOptions),
  ]);
}

function parseIdentityShell(raw: string | null): StoredIdentityShell | null {
  if (!raw) {
    return null;
  }

  try {
    const parsed: unknown = JSON.parse(raw);
    if (!parsed || typeof parsed !== 'object') {
      return null;
    }

    const rec = parsed as Record<string, unknown>;
    if (typeof rec.displayName !== 'string' || rec.displayName.trim().length === 0) {
      return null;
    }
    if (typeof rec.memberId !== 'string' || rec.memberId.trim().length === 0) {
      return null;
    }

    return {
      displayName: rec.displayName,
      memberId: rec.memberId,
      avatarPath: typeof rec.avatarPath === 'string' ? rec.avatarPath : null,
    };
  } catch {
    return null;
  }
}
