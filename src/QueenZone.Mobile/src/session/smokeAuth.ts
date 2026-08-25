/**
 * Debug-only smoke session injection (#872, tightened by #831 Option A).
 *
 * Enabled only when `__DEV__ === true` and `appEnv === 'development'`.
 * Staging/production fail closed even if a Debug client is pointed at
 * those origins. Release builds also compile `__DEV__` to false.
 * `applySmokeSession` no-ops when this returns false.
 */

export const smokeAuthScheme = 'queenzone';
export const smokeAuthHost = 'smoke-auth';
export const smokeAuthRefreshPlaceholder = 'smoke-debug-no-refresh';
export const smokeAuthExpiresInSeconds = 3600;

export type SmokeAuthGate = {
  /** Metro/Release compile flag. Missing or false fails closed. */
  dev?: boolean;
  /** Runtime app environment. Anything other than development fails closed. */
  appEnv?: string;
};

export function isSmokeAuthEnabled(env: SmokeAuthGate = {}): boolean {
  const dev = env.dev ?? (typeof __DEV__ !== 'undefined' ? __DEV__ : false);
  return dev === true && env.appEnv === 'development';
}

export function parseSmokeAuthAccessToken(url: string): string | null {
  const parsed = tryParseUrl(url);
  if (!parsed) {
    return null;
  }

  const hostOrPath = parsed.hostname || parsed.host;
  const path = parsed.pathname.replace(/^\/+/, '');
  const isSmoke =
    hostOrPath === smokeAuthHost || path === smokeAuthHost || parsed.pathname === `/${smokeAuthHost}`;
  if (parsed.protocol !== `${smokeAuthScheme}:` || !isSmoke) {
    return null;
  }

  const token = parsed.searchParams.get('accessToken')?.trim() ?? '';
  return token.length > 0 ? token : null;
}

export function buildSmokeAuthUrl(accessToken: string): string {
  const token = accessToken.trim();
  if (!token) {
    throw new Error('Smoke auth URL requires a non-empty access token.');
  }

  return `${smokeAuthScheme}://${smokeAuthHost}?accessToken=${encodeURIComponent(token)}`;
}

function tryParseUrl(url: string): URL | null {
  try {
    return new URL(url);
  } catch {
    return null;
  }
}
