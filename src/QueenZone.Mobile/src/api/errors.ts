import type { ProblemDetails } from './types';

export type ApiFailureKind = 'timeout' | 'offline' | 'http' | 'malformed';

export const TIMEOUT_MESSAGE =
  'QueenZone is taking too long to respond. Check your connection and try again.';

export const OFFLINE_MESSAGE = 'Unable to reach QueenZone. Check your connection and try again.';

export const MALFORMED_MESSAGE = 'QueenZone sent a response we could not read.';

export class ApiError extends Error {
  readonly kind: ApiFailureKind;
  readonly status: number;
  readonly problem: ProblemDetails | null;

  constructor(status: number, message: string, problem: ProblemDetails | null = null, kind?: ApiFailureKind) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.problem = problem;
    this.kind = kind ?? (status === 0 ? 'offline' : 'http');
  }

  static timeout(): ApiError {
    return new ApiError(0, TIMEOUT_MESSAGE, null, 'timeout');
  }

  static offline(): ApiError {
    return new ApiError(0, OFFLINE_MESSAGE, null, 'offline');
  }

  static http(status: number, message: string, problem: ProblemDetails | null = null): ApiError {
    return new ApiError(status, message, problem, 'http');
  }

  static malformed(status: number): ApiError {
    return new ApiError(status, MALFORMED_MESSAGE, null, 'malformed');
  }
}

export function isTimeoutFailure(err: unknown): err is ApiError & { kind: 'timeout' } {
  return err instanceof ApiError && err.kind === 'timeout';
}

export function isOfflineFailure(err: unknown): err is ApiError & { kind: 'offline' } {
  return err instanceof ApiError && err.kind === 'offline';
}
