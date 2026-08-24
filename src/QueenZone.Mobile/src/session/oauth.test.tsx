import * as Crypto from 'expo-crypto';
import * as WebBrowser from 'expo-web-browser';
import { logoutRemote, refreshAccessToken, revokeRefreshToken, signInWithProvider } from './oauth';
import { jsonResponse } from '../test/fixtures';

jest.mock('expo-web-browser', () => ({
  maybeCompleteAuthSession: jest.fn(),
  openAuthSessionAsync: jest.fn(),
}));

jest.mock('expo-crypto', () => ({
  CryptoDigestAlgorithm: { SHA256: 'SHA-256' },
  CryptoEncoding: { BASE64: 'base64' },
  getRandomBytesAsync: jest.fn(async (n: number) => Uint8Array.from({ length: n }, (_, i) => i + 1)),
  digestStringAsync: jest.fn(async () => 'abc+def/ghi='),
}));

const openAuth = WebBrowser.openAuthSessionAsync as jest.MockedFunction<
  typeof WebBrowser.openAuthSessionAsync
>;
const fetchMock = jest.fn<Promise<Response>, [RequestInfo | URL, RequestInit?]>();

beforeEach(() => {
  fetchMock.mockReset();
  global.fetch = fetchMock as unknown as typeof fetch;
  (Crypto.getRandomBytesAsync as jest.Mock).mockClear();
});

function authorizeState(url: string) {
  return new URL(url).searchParams.get('state') ?? '';
}

describe('signInWithProvider', () => {
  it('exchanges a successful callback for tokens', async () => {
    openAuth.mockImplementation(async (url) => ({
      type: 'success',
      url: `queenzone://auth/callback?code=auth-code&state=${authorizeState(String(url))}`,
    }));
    fetchMock.mockResolvedValueOnce(
      jsonResponse({ access_token: 'a', refresh_token: 'r', expires_in: 900 }),
    );

    const tokens = await signInWithProvider('http://qz.test', 'Google');
    expect(tokens).toEqual({ accessToken: 'a', refreshToken: 'r', expiresIn: 900 });
    expect(String(fetchMock.mock.calls[0]?.[0])).toContain('/api/v1/auth/token');
  });

  it('maps cancel, missing code, state mismatch, and provider errors', async () => {
    openAuth.mockResolvedValueOnce({ type: 'cancel' });
    await expect(signInWithProvider('http://qz.test', 'Google')).rejects.toThrow('Sign-in was cancelled.');

    openAuth.mockResolvedValueOnce({
      type: 'success',
      url: 'queenzone://auth/callback?state=nope',
    });
    await expect(signInWithProvider('http://qz.test', 'Google')).rejects.toThrow(
      'Sign-in did not return an authorization code.',
    );

    openAuth.mockImplementationOnce(async (url) => ({
      type: 'success',
      url: `queenzone://auth/callback?code=x&state=${authorizeState(String(url))}x`,
    }));
    await expect(signInWithProvider('http://qz.test', 'Google')).rejects.toThrow('Sign-in state mismatch.');

    openAuth.mockResolvedValueOnce({
      type: 'success',
      url: 'queenzone://auth/callback?error=access_denied&error_description=Nope',
    });
    await expect(signInWithProvider('http://qz.test', 'Google')).rejects.toThrow('Nope');
  });
});

describe('token maintenance', () => {
  it('refreshes an access token', async () => {
    fetchMock.mockResolvedValueOnce(
      jsonResponse({ access_token: 'a2', refresh_token: 'r2', expires_in: 600 }),
    );
    await expect(refreshAccessToken('http://qz.test', 'r')).resolves.toEqual({
      accessToken: 'a2',
      refreshToken: 'r2',
      expiresIn: 600,
    });
  });

  it('treats logout and revoke as best-effort', async () => {
    fetchMock.mockRejectedValue(new TypeError('offline'));
    await expect(logoutRemote('http://qz.test', 'a')).resolves.toBeUndefined();
    await expect(revokeRefreshToken('http://qz.test', 'r')).resolves.toBeUndefined();
  });
});
