import * as SecureStore from 'expo-secure-store';
import type { AuthTokens } from '../api/auth';
import { getAppConfig } from '../config/appConfig';

/**
 * Key names written before grants were scoped to an API origin.
 * Read once, migrated into the current scope, then deleted.
 */
const legacyKeys = {
  access: 'queenzone.mobile.accessToken',
  refresh: 'queenzone.mobile.refreshToken',
  expiry: 'queenzone.mobile.accessExpiresAt',
  identity: 'queenzone.mobile.identityShell',
} as const;

type SessionKeys = { access: string; refresh: string; expiry: string; identity: string };

/**
 * A grant is only valid at the origin that issued it, and TestFlight ships
 * staging and production builds under one bundle id — so an unscoped key let a
 * `dev.queenzone.org` refresh token reach `www.queenzone.org`, where it is an
 * unknown hash and comes back `invalid_grant`. Scoping keeps both sessions and
 * makes switching between those builds a no-op instead of a silent sign-out.
 *
 * SecureStore keys accept `[A-Za-z0-9._-]` only, so the origin is sanitised.
 */
function apiScope(): string {
  return getAppConfig()
    .apiBaseUrl.replace(/^https?:\/\//i, '')
    .replace(/[^\w.-]+/g, '_');
}

function scopedKeys(): SessionKeys {
  const scope = apiScope();
  return {
    access: `queenzone.mobile.${scope}.accessToken`,
    refresh: `queenzone.mobile.${scope}.refreshToken`,
    expiry: `queenzone.mobile.${scope}.accessExpiresAt`,
    identity: `queenzone.mobile.${scope}.identityShell`,
  };
}

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

type RawSession = {
  accessToken: string | null;
  refreshToken: string | null;
  expiry: string | null;
  identityRaw: string | null;
};

async function readKeys(keys: SessionKeys): Promise<RawSession> {
  const [accessToken, refreshToken, expiry, identityRaw] = await Promise.all([
    SecureStore.getItemAsync(keys.access, sessionStoreOptions),
    SecureStore.getItemAsync(keys.refresh, sessionStoreOptions),
    SecureStore.getItemAsync(keys.expiry, sessionStoreOptions),
    SecureStore.getItemAsync(keys.identity, sessionStoreOptions),
  ]);
  return { accessToken, refreshToken, expiry, identityRaw };
}

/**
 * Adopt a pre-scope grant into the running build's scope. Best effort: the grant
 * is usable either way, and a failed copy just repeats on the next launch.
 */
async function migrateLegacySession(raw: RawSession): Promise<void> {
  const keys = scopedKeys();
  try {
    await Promise.all([
      raw.accessToken ? writeSessionItem(keys.access, raw.accessToken) : Promise.resolve(),
      raw.refreshToken ? writeSessionItem(keys.refresh, raw.refreshToken) : Promise.resolve(),
      raw.expiry ? writeSessionItem(keys.expiry, raw.expiry) : Promise.resolve(),
      raw.identityRaw ? writeSessionItem(keys.identity, raw.identityRaw) : Promise.resolve(),
    ]);
    await clearLegacySession();
  } catch {
    // Keep the legacy copy so the next launch can try again.
  }
}

export async function readStoredSession(): Promise<StoredSession | null> {
  try {
    let raw = await readKeys(scopedKeys());
    if (!raw.accessToken || !raw.refreshToken) {
      const legacy = await readKeys(legacyKeys);
      if (legacy.accessToken && legacy.refreshToken) {
        await migrateLegacySession(legacy);
        raw = legacy;
      }
    }

    const { accessToken, refreshToken, expiry, identityRaw } = raw;
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
  const keys = scopedKeys();
  try {
    await Promise.all([
      writeSessionItem(keys.access, tokens.accessToken),
      writeSessionItem(keys.refresh, tokens.refreshToken),
      writeSessionItem(keys.expiry, String(expiresAt)),
    ]);
  } catch (error) {
    rethrowKeychainError(error);
  }
  // The scoped copy is authoritative now; an orphaned legacy grant must not be
  // picked up later by a build pointed at a different origin.
  await clearLegacySession();
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
    await writeSessionItem(scopedKeys().identity, JSON.stringify(payload));
  } catch (error) {
    rethrowKeychainError(error);
  }
}

async function clearLegacySession(): Promise<void> {
  try {
    await Promise.all(
      Object.values(legacyKeys).map((key) => SecureStore.deleteItemAsync(key, sessionStoreOptions)),
    );
  } catch {
    // Nothing to recover: the scoped copy is what gets read.
  }
}

export async function clearStoredSession(): Promise<void> {
  await Promise.all([
    ...Object.values(scopedKeys()).map((key) =>
      SecureStore.deleteItemAsync(key, sessionStoreOptions),
    ),
    clearLegacySession(),
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
