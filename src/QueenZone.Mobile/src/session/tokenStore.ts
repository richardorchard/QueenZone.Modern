import * as SecureStore from 'expo-secure-store';
import type { AuthTokens } from '../api/auth';

const accessKey = 'queenzone.mobile.accessToken';
const refreshKey = 'queenzone.mobile.refreshToken';
const expiryKey = 'queenzone.mobile.accessExpiresAt';
/** Sibling of the grant. Stable across store/TestFlight binaries — not version-namespaced. */
const identityKey = 'queenzone.mobile.identityShell';

export type StoredIdentityShell = {
  displayName: string;
  memberId: string;
  avatarPath?: string | null;
};

export type StoredSession = AuthTokens & {
  expiresAt: number;
  identity?: StoredIdentityShell | null;
};

export async function readStoredSession(): Promise<StoredSession | null> {
  const [accessToken, refreshToken, expiry, identityRaw] = await Promise.all([
    SecureStore.getItemAsync(accessKey),
    SecureStore.getItemAsync(refreshKey),
    SecureStore.getItemAsync(expiryKey),
    SecureStore.getItemAsync(identityKey),
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
}

export async function writeStoredSession(tokens: AuthTokens): Promise<StoredSession> {
  const expiresAt = Date.now() + Math.max(tokens.expiresIn - 30, 30) * 1000;
  await Promise.all([
    SecureStore.setItemAsync(accessKey, tokens.accessToken),
    SecureStore.setItemAsync(refreshKey, tokens.refreshToken),
    SecureStore.setItemAsync(expiryKey, String(expiresAt)),
  ]);
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
  await SecureStore.setItemAsync(identityKey, JSON.stringify(payload));
}

export async function clearStoredSession(): Promise<void> {
  await Promise.all([
    SecureStore.deleteItemAsync(accessKey),
    SecureStore.deleteItemAsync(refreshKey),
    SecureStore.deleteItemAsync(expiryKey),
    SecureStore.deleteItemAsync(identityKey),
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
