import * as SecureStore from 'expo-secure-store';
import type { AuthTokens } from '../api/auth';

const accessKey = 'queenzone.mobile.accessToken';
const refreshKey = 'queenzone.mobile.refreshToken';
const expiryKey = 'queenzone.mobile.accessExpiresAt';

export type StoredSession = AuthTokens & {
  expiresAt: number;
};

export async function readStoredSession(): Promise<StoredSession | null> {
  const [accessToken, refreshToken, expiry] = await Promise.all([
    SecureStore.getItemAsync(accessKey),
    SecureStore.getItemAsync(refreshKey),
    SecureStore.getItemAsync(expiryKey),
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

export async function clearStoredSession(): Promise<void> {
  await Promise.all([
    SecureStore.deleteItemAsync(accessKey),
    SecureStore.deleteItemAsync(refreshKey),
    SecureStore.deleteItemAsync(expiryKey),
  ]);
}
