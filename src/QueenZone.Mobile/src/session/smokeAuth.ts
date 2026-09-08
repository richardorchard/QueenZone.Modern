/**
 * Smoke session injection (#872, tightened by #831 Option A, #1322).
 *
 * Enabled when `appEnv === 'development'` and either `__DEV__` is true
 * (local Debug) or `smokeEmbed` was baked at prebuild (CI Release embed).
 * Staging/production fail closed even if a Debug client is pointed at
 * those origins. Store Release compiles `__DEV__` to false and does not
 * bake `smokeEmbed`. `applySmokeSession` no-ops when this returns false.
 */

export const smokeAuthScheme = 'queenzone';
export const smokeAuthHost = 'smoke-auth';
export const smokeAuthRefreshPlaceholder = 'smoke-debug-no-refresh';
export const smokeAuthExpiresInSeconds = 3600;

export type SmokeAuthGate = {
  /** Metro/Release compile flag. Missing or false fails closed unless smokeEmbed. */
  dev?: boolean;
  /** Runtime app environment. Anything other than development fails closed. */
  appEnv?: string;
  /** Baked QUEENZONE_MOBILE_SMOKE_EMBED so Release Testing binaries can inject. */
  smokeEmbed?: boolean;
};

export function isSmokeAuthEnabled(env: SmokeAuthGate = {}): boolean {
  const dev = env.dev ?? (typeof __DEV__ !== 'undefined' ? __DEV__ : false);
  const embed = env.smokeEmbed === true;
  return (dev === true || embed) && env.appEnv === 'development';
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
