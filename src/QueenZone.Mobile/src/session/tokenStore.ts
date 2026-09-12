import * as SecureStore from 'expo-secure-store';
import type { AuthTokens } from '../api/auth';

/** Single key for the whole grant so a killed process can never tear it into a partial state. */
const grantKey = 'queenzone.mobile.grant';
/** Legacy per-field keys, read once for migration and then deleted. Do not write to these. */
const legacyAccessKey = 'queenzone.mobile.accessToken';
const legacyRefreshKey = 'queenzone.mobile.refreshToken';
const legacyExpiryKey = 'queenzone.mobile.accessExpiresAt';
/** Sibling of the grant. Stable across store/TestFlight binaries — not version-namespaced. */
const identityKey = 'queenzone.mobile.identityShell';

type StoredGrant = {
  accessToken: string;
  refreshToken: string;
  expiresAt: number;
};

/** Shared iOS accessibility for the session keys. Do not add requireAuthentication. */
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

/**
 * Reads the legacy per-field grant and, if present, migrates it to the combined
 * key so a torn write can no longer happen. Returns null if there is no legacy
 * grant to migrate. Callers must already hold the read/write try block for
 * Keychain-locked handling.
 */
async function migrateLegacyGrant(): Promise<StoredGrant | null> {
  const [accessToken, refreshToken, expiry] = await Promise.all([
    SecureStore.getItemAsync(legacyAccessKey, sessionStoreOptions),
    SecureStore.getItemAsync(legacyRefreshKey, sessionStoreOptions),
    SecureStore.getItemAsync(legacyExpiryKey, sessionStoreOptions),
  ]);
  if (!accessToken || !refreshToken) {
    return null;
  }

  const parsedExpiry = expiry ? Number.parseInt(expiry, 10) : 0;
  const grant: StoredGrant = {
    accessToken,
    refreshToken,
    expiresAt: Number.isFinite(parsedExpiry) ? parsedExpiry : 0,
  };
  await writeSessionItem(grantKey, JSON.stringify(grant));
  await Promise.all([
    SecureStore.deleteItemAsync(legacyAccessKey, sessionStoreOptions),
    SecureStore.deleteItemAsync(legacyRefreshKey, sessionStoreOptions),
    SecureStore.deleteItemAsync(legacyExpiryKey, sessionStoreOptions),
  ]);
  return grant;
}

function parseGrant(raw: string | null): StoredGrant | null {
  if (!raw) {
    return null;
  }

  try {
    const parsed: unknown = JSON.parse(raw);
    if (!parsed || typeof parsed !== 'object') {
      return null;
    }

    const rec = parsed as Record<string, unknown>;
    if (typeof rec.accessToken !== 'string' || typeof rec.refreshToken !== 'string') {
      return null;
    }

    const expiresAt = typeof rec.expiresAt === 'number' && Number.isFinite(rec.expiresAt) ? rec.expiresAt : 0;
    return { accessToken: rec.accessToken, refreshToken: rec.refreshToken, expiresAt };
  } catch {
    return null;
  }
}

export async function readStoredSession(): Promise<StoredSession | null> {
  try {
    const [grantRaw, identityRaw] = await Promise.all([
      SecureStore.getItemAsync(grantKey, sessionStoreOptions),
      SecureStore.getItemAsync(identityKey, sessionStoreOptions),
    ]);

    const grant = parseGrant(grantRaw) ?? (await migrateLegacyGrant());
    if (!grant) {
      return null;
    }

    return {
      accessToken: grant.accessToken,
      refreshToken: grant.refreshToken,
      expiresIn: 900,
      expiresAt: grant.expiresAt,
      identity: parseIdentityShell(identityRaw),
    };
  } catch (error) {
    rethrowKeychainError(error);
  }
}

export async function writeStoredSession(tokens: AuthTokens): Promise<StoredSession> {
  const expiresAt = Date.now() + Math.max(tokens.expiresIn - 30, 30) * 1000;
  const grant: StoredGrant = { accessToken: tokens.accessToken, refreshToken: tokens.refreshToken, expiresAt };
  try {
    await writeSessionItem(grantKey, JSON.stringify(grant));
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
    SecureStore.deleteItemAsync(grantKey, sessionStoreOptions),
    SecureStore.deleteItemAsync(legacyAccessKey, sessionStoreOptions),
    SecureStore.deleteItemAsync(legacyRefreshKey, sessionStoreOptions),
    SecureStore.deleteItemAsync(legacyExpiryKey, sessionStoreOptions),
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
