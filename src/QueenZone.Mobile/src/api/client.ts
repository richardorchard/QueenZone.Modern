import { apiV1Url } from '../config';
import { reportApiFailure } from '../config/sentry';
import { ApiError, isTimeoutFailure } from './errors';
import {
  classifyXhrFailure,
  interpretMultipartXhrResult,
  postFormWithXhr,
} from './multipartXhr';
import { shouldUseNativeMultipartUpload } from './nativeUpload';
import type { ProblemDetails } from './types';

export { ApiError, isOfflineFailure, isTimeoutFailure } from './errors';
export type { ApiFailureKind } from './errors';

export type FetchJsonOptions = {
  signal?: AbortSignal;
  query?: Record<string, string | number | undefined | null>;
  /** Optional Bearer — poll GET fills viewer flags; writes require it. */
  accessToken?: string | null;
};

export type SendMultipartOptions = FetchJsonOptions & {
  /**
   * `auto` uses XMLHttpRequest on React Native (iOS fetch+FormData throws)
   * and fetch everywhere else. Tests may force either transport.
   */
  transport?: 'auto' | 'fetch' | 'xhr';
};

export type SendJsonOptions = FetchJsonOptions & {
  method?: 'POST' | 'PATCH' | 'PUT' | 'DELETE';
  body?: unknown;
  accessToken?: string | null;
};

const GET_ATTEMPT_MS = 12_000;
const GET_TOTAL_MS = 15_000;
const JSON_WRITE_MS = 15_000;
const MULTIPART_MS = 180_000;
const GET_MAX_ATTEMPTS = 2;
const RETRY_BASE_MS = 300;
const RETRY_CAP_MS = 1_500;
const RETRYABLE_HTTP = new Set([502, 503, 504]);

type HttpMethod = 'GET' | 'POST' | 'PATCH' | 'PUT' | 'DELETE';

type RequestPolicy = {
  attemptTimeoutMs: number;
  totalTimeoutMs: number;
  maxAttempts: number;
  write: boolean;
};

const GET_POLICY: RequestPolicy = {
  attemptTimeoutMs: GET_ATTEMPT_MS,
  totalTimeoutMs: GET_TOTAL_MS,
  maxAttempts: GET_MAX_ATTEMPTS,
  write: false,
};

const JSON_WRITE_POLICY: RequestPolicy = {
  attemptTimeoutMs: JSON_WRITE_MS,
  totalTimeoutMs: JSON_WRITE_MS,
  maxAttempts: 1,
  write: true,
};

const MULTIPART_POLICY: RequestPolicy = {
  attemptTimeoutMs: MULTIPART_MS,
  totalTimeoutMs: MULTIPART_MS,
  maxAttempts: 1,
  write: true,
};

type DeadlineHandle = {
  readonly signal: AbortSignal;
  timedOut(): boolean;
  dispose(): void;
};

function buildUrl(path: string, query?: FetchJsonOptions['query']): string {
  const url = apiV1Url(path);
  if (!query) {
    return url;
  }

  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(query)) {
    if (value === undefined || value === null || value === '') {
      continue;
    }
    params.set(key, String(value));
  }

  const qs = params.toString();
  return qs ? `${url}?${qs}` : url;
}

function messageFromProblem(status: number, problem: ProblemDetails | null, fallback: string): string {
  if (problem?.detail?.trim()) {
    return problem.detail.trim();
  }
  if (problem?.title?.trim()) {
    return problem.title.trim();
  }
  if (status === 404) {
    return 'Not found.';
  }
  if (status >= 500) {
    return 'The server had a problem. Try again shortly.';
  }
  return fallback;
}

async function readProblem(response: Response): Promise<ProblemDetails | null> {
  const contentType = response.headers.get('content-type') ?? '';
  if (!contentType.includes('json')) {
    return null;
  }
  try {
    return (await response.json()) as ProblemDetails;
  } catch {
    return null;
  }
}

function messageForWriteStatus(status: number): string {
  if (status === 401) {
    return 'Sign in to continue.';
  }
  if (status === 403) {
    return 'You cannot do that.';
  }
  if (status === 429) {
    return "You're posting too quickly. Please wait a bit and try again.";
  }
  return `Request failed (${status}).`;
}

function isCallerAbort(err: unknown): err is Error & { name: 'AbortError' } {
  return err instanceof Error && err.name === 'AbortError';
}

function composeDeadline(caller: AbortSignal | undefined, timeoutMs: number): DeadlineHandle {
  const controller = new AbortController();
  let timedOut = false;

  const onCallerAbort = () => {
    controller.abort();
  };

  if (caller?.aborted) {
    controller.abort();
    return {
      signal: controller.signal,
      timedOut: () => false,
      dispose: () => {},
    };
  }

  caller?.addEventListener('abort', onCallerAbort);
  const timer = setTimeout(() => {
    timedOut = true;
    controller.abort();
  }, timeoutMs);

  return {
    signal: controller.signal,
    timedOut: () => timedOut && !caller?.aborted,
    dispose: () => {
      clearTimeout(timer);
      caller?.removeEventListener('abort', onCallerAbort);
    },
  };
}

function classifyFetchFailure(err: unknown, deadline: DeadlineHandle, caller?: AbortSignal): unknown {
  if (caller?.aborted) {
    return isCallerAbort(err) ? err : Object.assign(new Error('Aborted'), { name: 'AbortError' });
  }
  if (isCallerAbort(err)) {
    return deadline.timedOut() ? ApiError.timeout(err) : err;
  }
  return ApiError.offline(err);
}

function shouldRetryGet(err: unknown): boolean {
  if (isTimeoutFailure(err)) {
    return true;
  }
  return err instanceof ApiError && err.kind === 'http' && RETRYABLE_HTTP.has(err.status);
}

function computeGetRetryDelayMs(failedIndex: number): number {
  const cap = Math.min(RETRY_CAP_MS, RETRY_BASE_MS * 2 ** failedIndex);
  return Math.floor(Math.random() * (cap + 1));
}

function abortableSleep(ms: number, signal?: AbortSignal): Promise<void> {
  if (ms <= 0) {
    return Promise.resolve();
  }
  if (signal?.aborted) {
    return Promise.reject(Object.assign(new Error('Aborted'), { name: 'AbortError' }));
  }
  return new Promise((resolve, reject) => {
    const timer = setTimeout(() => {
      signal?.removeEventListener('abort', onAbort);
      resolve();
    }, ms);
    const onAbort = () => {
      clearTimeout(timer);
      reject(Object.assign(new Error('Aborted'), { name: 'AbortError' }));
    };
    signal?.addEventListener('abort', onAbort);
  });
}

function reportTerminal(err: unknown, method: HttpMethod, path: string): void {
  if (!(err instanceof ApiError)) {
    return;
  }
  reportApiFailure({
    kind: err.kind,
    status: err.status,
    method,
    path,
    cause: err.cause,
  });
}

function authHeaders(accessToken?: string | null): Record<string, string> {
  const headers: Record<string, string> = { Accept: 'application/json' };
  if (accessToken) {
    headers.Authorization = `Bearer ${accessToken}`;
  }
  return headers;
}

async function request<T>(input: {
  method: HttpMethod;
  path: string;
  url: string;
  headers: Record<string, string>;
  body?: string | FormData;
  signal?: AbortSignal;
  policy: RequestPolicy;
}): Promise<T> {
  const startedAt = Date.now();
  let lastError: unknown;

  for (let attempt = 0; attempt < input.policy.maxAttempts; attempt++) {
    if (attempt > 0) {
      if (input.signal?.aborted) {
        throw Object.assign(new Error('Aborted'), { name: 'AbortError' });
      }
      const delay = computeGetRetryDelayMs(attempt - 1);
      const remaining = input.policy.totalTimeoutMs - (Date.now() - startedAt);
      const nextAttemptMs = Math.min(input.policy.attemptTimeoutMs, remaining - delay);
      if (nextAttemptMs <= 0) {
        reportTerminal(lastError, input.method, input.path);
        throw lastError;
      }
      await abortableSleep(delay, input.signal);
    }

    const remaining = input.policy.totalTimeoutMs - (Date.now() - startedAt);
    const attemptMs = Math.min(input.policy.attemptTimeoutMs, remaining);
    if (attemptMs <= 0) {
      const error = lastError ?? ApiError.timeout();
      reportTerminal(error, input.method, input.path);
      throw error;
    }

    const deadline = composeDeadline(input.signal, attemptMs);
    try {
      let response: Response;
      try {
        response = await fetch(input.url, {
          method: input.method,
          headers: input.headers,
          body: input.body,
          signal: deadline.signal,
        });
      } catch (err) {
        const classified = classifyFetchFailure(err, deadline, input.signal);
        lastError = classified;
        if (
          input.policy.maxAttempts > 1 &&
          attempt < input.policy.maxAttempts - 1 &&
          shouldRetryGet(classified)
        ) {
          continue;
        }
        reportTerminal(classified, input.method, input.path);
        throw classified;
      }

      if (!response.ok) {
        const problem = await readProblem(response);
        const fallback = input.policy.write
          ? messageForWriteStatus(response.status)
          : `Request failed (${response.status}).`;
        const httpError = ApiError.http(
          response.status,
          messageFromProblem(response.status, problem, fallback),
          problem,
        );
        lastError = httpError;
        if (
          input.policy.maxAttempts > 1 &&
          attempt < input.policy.maxAttempts - 1 &&
          shouldRetryGet(httpError)
        ) {
          continue;
        }
        reportTerminal(httpError, input.method, input.path);
        throw httpError;
      }

      if (response.status === 204) {
        return undefined as T;
      }

      try {
        return (await response.json()) as T;
      } catch {
        const malformed = ApiError.malformed(response.status);
        reportTerminal(malformed, input.method, input.path);
        throw malformed;
      }
    } finally {
      deadline.dispose();
    }
  }

  reportTerminal(lastError, input.method, input.path);
  throw lastError;
}

/**
 * GET JSON from `/api/v1{path}` with optional query string.
 * Throws {@link ApiError} for non-2xx responses (Problem Details when present).
 */
export async function fetchJson<T>(path: string, options: FetchJsonOptions = {}): Promise<T> {
  return request<T>({
    method: 'GET',
    path,
    url: buildUrl(path, options.query),
    headers: authHeaders(options.accessToken),
    signal: options.signal,
    policy: GET_POLICY,
  });
}

/**
 * JSON write to `/api/v1{path}`. Sends `Authorization: Bearer` when
 * `accessToken` is present. Throws {@link ApiError} for non-2xx responses.
 */
export async function sendJson<T>(path: string, options: SendJsonOptions = {}): Promise<T> {
  const headers = authHeaders(options.accessToken);
  if (options.body !== undefined) {
    headers['Content-Type'] = 'application/json';
  }
  return request<T>({
    method: options.method ?? 'POST',
    path,
    url: buildUrl(path, options.query),
    headers,
    body: options.body === undefined ? undefined : JSON.stringify(options.body),
    signal: options.signal,
    policy: JSON_WRITE_POLICY,
  });
}

function useXhrTransport(options: SendMultipartOptions): boolean {
  if (options.transport === 'xhr') {
    return true;
  }
  if (options.transport === 'fetch') {
    return false;
  }
  return shouldUseNativeMultipartUpload();
}

/**
 * Multipart write to `/api/v1{path}` (avatar upload, photo submissions).
 * Do not set Content-Type; the transport supplies the multipart boundary.
 * React Native uses XMLHttpRequest because `fetch`+FormData file parts
 * throw `TypeError: Network request failed` on some iOS TestFlight builds.
 */
export async function sendMultipart<T>(
  path: string,
  formData: FormData,
  options: SendMultipartOptions = {},
): Promise<T> {
  if (useXhrTransport(options)) {
    const url = buildUrl(path, options.query);
    try {
      const result = await postFormWithXhr({
        url,
        formData,
        headers: authHeaders(options.accessToken),
        timeoutMs: MULTIPART_POLICY.attemptTimeoutMs,
        signal: options.signal,
      });
      return interpretMultipartXhrResult<T>(result, messageForWriteStatus, messageFromProblem);
    } catch (err) {
      const classified = classifyXhrFailure(err, options.signal);
      if (classified instanceof ApiError) {
        reportTerminal(classified, 'POST', path);
      }
      throw classified;
    }
  }

  return request<T>({
    method: 'POST',
    path,
    url: buildUrl(path, options.query),
    headers: authHeaders(options.accessToken),
    body: formData,
    signal: options.signal,
    policy: MULTIPART_POLICY,
  });
}

export { formatPublishedDate, toPlainText } from './text';
