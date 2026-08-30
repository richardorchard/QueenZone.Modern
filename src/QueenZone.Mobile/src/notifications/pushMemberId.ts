/**
 * Owner of a push registration. Used so an unchanged native APNs/FCM token
 * still re-registers after an account switch (#1094).
 *
 * Prefer the explicit `/me` member id. Fall back to the access-token `sub`
 * so registration can run before the profile request returns.
 */
export function resolvePushMemberId(accessToken: string, memberId?: string | null): string | null {
  const explicit = memberId?.trim();
  if (explicit) {
    return explicit;
  }

  return readJwtSubject(accessToken);
}

export function readJwtSubject(accessToken: string): string | null {
  const parts = accessToken.split('.');
  if (parts.length !== 3 || !parts[1]) {
    return null;
  }

  try {
    const normalized = parts[1].replace(/-/g, '+').replace(/_/g, '/');
    const padded = normalized + '='.repeat((4 - (normalized.length % 4)) % 4);
    const parsed: unknown = JSON.parse(globalThis.atob(padded));
    if (!parsed || typeof parsed !== 'object') {
      return null;
    }

    const sub = (parsed as { sub?: unknown }).sub;
    return typeof sub === 'string' && sub.trim().length > 0 ? sub.trim() : null;
  } catch {
    return null;
  }
}
