/**
 * Debug-only smoke session injection (#872 Option A).
 *
 * Production / staging Release builds compile `__DEV__` to false, so this
 * handler is never registered there. The deep link is also an explicit
 * opt-in — it does not bypass sign-in unless something opens
 * `queenzone://smoke-auth?accessToken=…`.
 */

export const smokeAuthScheme = 'queenzone';
export const smokeAuthHost = 'smoke-auth';
export const smokeAuthRefreshPlaceholder = 'smoke-debug-no-refresh';
export const smokeAuthExpiresInSeconds = 3600;

export function isSmokeAuthEnabled(
  env: { dev?: boolean } = {
    dev: typeof __DEV__ !== 'undefined' ? __DEV__ : false,
  },
): boolean {
  return env.dev === true;
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
