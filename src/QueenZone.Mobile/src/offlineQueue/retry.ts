import { ApiError, isOfflineFailure, isTimeoutFailure } from '../api/errors';

const BACKOFF_BASE_MS = 2_000;
const BACKOFF_CAP_MS = 5 * 60_000;
const MAX_TRANSIENT_ATTEMPTS = 12;

export type QueueFailureClass = 'retry' | 'permanent' | 'auth' | 'systemic';

export function classifyQueueFailure(err: unknown): QueueFailureClass {
  if (isOfflineFailure(err) || isTimeoutFailure(err)) {
    return 'retry';
  }
  if (!(err instanceof ApiError) || err.kind !== 'http') {
    return 'retry';
  }
  if (err.status === 401) {
    return 'auth';
  }
  if (err.status === 429 || err.status >= 500) {
    return 'systemic';
  }
  if (err.status >= 400 && err.status < 500) {
    return 'permanent';
  }
  return 'retry';
}

export function nextRetryAt(attemptCount: number, err: unknown, now = Date.now()): string {
  const retryAfter = err instanceof ApiError ? err.retryAfterMs : null;
  if (typeof retryAfter === 'number' && retryAfter > 0) {
    return new Date(now + retryAfter).toISOString();
  }
  const exp = Math.min(BACKOFF_CAP_MS, BACKOFF_BASE_MS * 2 ** Math.max(0, attemptCount - 1));
  const jitter = Math.floor(Math.random() * (exp + 1));
  return new Date(now + jitter).toISOString();
}

export function exhaustedRetries(attemptCount: number): boolean {
  return attemptCount >= MAX_TRANSIENT_ATTEMPTS;
}
