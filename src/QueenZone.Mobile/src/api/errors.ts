import type { ProblemDetails } from './types';

export type ApiFailureKind = 'timeout' | 'offline' | 'http' | 'malformed' | 'local-file';

export const TIMEOUT_MESSAGE =
  'QueenZone is taking too long to respond. Check your connection and try again.';

export const OFFLINE_MESSAGE = 'Unable to reach QueenZone. Check your connection and try again.';

export const MALFORMED_MESSAGE = 'QueenZone sent a response we could not read.';

export const LOCAL_FILE_MESSAGE = 'Could not read the selected photo. Try choosing it again.';

export class ApiError extends Error {
  readonly kind: ApiFailureKind;
  readonly status: number;
  readonly problem: ProblemDetails | null;

  constructor(
    status: number,
    message: string,
    problem: ProblemDetails | null = null,
    kind?: ApiFailureKind,
    cause?: unknown,
  ) {
    super(message, cause === undefined ? undefined : { cause });
    this.name = 'ApiError';
    this.status = status;
    this.problem = problem;
    this.kind = kind ?? (status === 0 ? 'offline' : 'http');
  }

  static timeout(cause?: unknown): ApiError {
    return new ApiError(0, TIMEOUT_MESSAGE, null, 'timeout', cause);
  }

  static offline(cause?: unknown): ApiError {
    return new ApiError(0, OFFLINE_MESSAGE, null, 'offline', cause);
  }

  static localFile(cause?: unknown): ApiError {
    return new ApiError(0, LOCAL_FILE_MESSAGE, null, 'local-file', cause);
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

export function isLocalFileFailure(err: unknown): err is ApiError & { kind: 'local-file' } {
  return err instanceof ApiError && err.kind === 'local-file';
}
