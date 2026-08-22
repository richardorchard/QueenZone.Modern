import * as Crypto from 'expo-crypto';
import * as WebBrowser from 'expo-web-browser';
import {
  buildAuthorizeUrl,
  mobileClientId,
  mobileRedirectUri,
  parseAuthCallback,
  parseTokenResponse,
  tokenFormBody,
  authLogoutUrl,
  authRevokeUrl,
  authTokenUrl,
  type AuthTokens,
} from '../api/auth';

WebBrowser.maybeCompleteAuthSession();

export async function signInWithProvider(apiBaseUrl: string, provider: string): Promise<AuthTokens> {
  const state = await randomUrlSafe(16);
  const verifier = await randomUrlSafe(32);
  const challenge = await sha256Base64Url(verifier);
  const authorizeUrl = buildAuthorizeUrl({
    apiBaseUrl,
    provider,
    state,
    codeChallenge: challenge,
  });

  const result = await WebBrowser.openAuthSessionAsync(authorizeUrl, mobileRedirectUri);
  if (result.type !== 'success' || !('url' in result) || !result.url) {
    throw new Error(result.type === 'cancel' || result.type === 'dismiss' ? 'Sign-in was cancelled.' : 'Sign-in did not complete.');
  }

  const callback = parseAuthCallback(result.url);
  if (callback.error) {
    throw new Error(callback.errorDescription ?? callback.error);
  }

  if (!callback.code) {
    throw new Error('Sign-in did not return an authorization code.');
  }

  if (callback.state !== state) {
    throw new Error('Sign-in state mismatch.');
  }

  return exchangeAuthorizationCode(apiBaseUrl, callback.code, verifier);
}

export async function refreshAccessToken(apiBaseUrl: string, refreshToken: string): Promise<AuthTokens> {
  return postToken(apiBaseUrl, {
    grant_type: 'refresh_token',
    client_id: mobileClientId,
    refresh_token: refreshToken,
  });
}

export async function revokeRefreshToken(apiBaseUrl: string, refreshToken: string): Promise<void> {
  try {
    await fetch(authRevokeUrl(apiBaseUrl), {
      method: 'POST',
      headers: { 'Content-Type': 'application/x-www-form-urlencoded', Accept: 'application/json' },
      body: tokenFormBody({ token: refreshToken, client_id: mobileClientId }),
    });
  } catch {
    // RFC 7009: revocation is best-effort.
  }
}

export async function logoutRemote(apiBaseUrl: string, accessToken: string): Promise<void> {
  try {
    await fetch(authLogoutUrl(apiBaseUrl), {
      method: 'POST',
      headers: { Accept: 'application/json', Authorization: `Bearer ${accessToken}` },
    });
  } catch {
    // Local sign-out still proceeds.
  }
}

async function exchangeAuthorizationCode(
  apiBaseUrl: string,
  code: string,
  verifier: string,
): Promise<AuthTokens> {
  return postToken(apiBaseUrl, {
    grant_type: 'authorization_code',
    client_id: mobileClientId,
    redirect_uri: mobileRedirectUri,
    code,
    code_verifier: verifier,
  });
}

async function postToken(apiBaseUrl: string, fields: Record<string, string>): Promise<AuthTokens> {
  const response = await fetch(authTokenUrl(apiBaseUrl), {
    method: 'POST',
    headers: {
      Accept: 'application/json',
      'Content-Type': 'application/x-www-form-urlencoded',
    },
    body: tokenFormBody(fields),
  });
  const payload: unknown = await response.json().catch(() => null);
  if (!response.ok) {
    const error = payload && typeof payload === 'object' ? (payload as { error_description?: unknown }) : null;
    const description =
      typeof error?.error_description === 'string' ? error.error_description : 'Could not complete sign-in.';
    throw new Error(description);
  }

  return parseTokenResponse(payload);
}

async function randomUrlSafe(byteLength: number): Promise<string> {
  const bytes = await Crypto.getRandomBytesAsync(byteLength);
  return base64Url(bytes);
}

async function sha256Base64Url(value: string): Promise<string> {
  const digest = await Crypto.digestStringAsync(Crypto.CryptoDigestAlgorithm.SHA256, value, {
    encoding: Crypto.CryptoEncoding.BASE64,
  });
  return digest.replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/g, '');
}

function base64Url(bytes: Uint8Array): string {
  let binary = '';
  for (const byte of bytes) {
    binary += String.fromCharCode(byte);
  }

  return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/g, '');
}
