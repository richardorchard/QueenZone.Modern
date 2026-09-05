import { ApiError, isOfflineFailure, isTimeoutFailure } from '../api/errors';

function errorText(err: unknown): string {
  if (err instanceof Error) {
    return `${err.name} ${err.message}`;
  }
  return typeof err === 'string' ? err : '';
}

/** Token endpoint said the grant is dead. Sign the member out. */
export function isDefiniteAuthRefreshFailure(err: unknown): boolean {
  if (err instanceof ApiError && (err.status === 401 || err.status === 400)) {
    return true;
  }

  return /invalid_grant|invalid_token|unauthorized/i.test(errorText(err));
}

/**
 * Network/timeout while refreshing. Keep the local member identity so
 * same-account downloads stay playable offline.
 */
export function isTransientRefreshFailure(err: unknown): boolean {
  if (isDefiniteAuthRefreshFailure(err)) {
    return false;
  }
  if (isOfflineFailure(err) || isTimeoutFailure(err)) {
    return true;
  }
  if (err instanceof TypeError) {
    return true;
  }

  return /offline|timeout|network|failed to fetch|network request failed|internet|connection|enotfound|econnrefused/i.test(
    errorText(err),
  );
}
