import { apiV1Url } from '../config';
import { ApiError } from './errors';
import type { ProblemDetails } from './types';

export { ApiError } from './errors';

export type FetchJsonOptions = {
  signal?: AbortSignal;
  query?: Record<string, string | number | undefined | null>;
  /** Optional Bearer — poll GET fills viewer flags; writes require it. */
  accessToken?: string | null;
};

export type SendJsonOptions = FetchJsonOptions & {
  method?: 'POST' | 'PATCH' | 'PUT' | 'DELETE';
  body?: unknown;
  accessToken?: string | null;
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

/**
 * GET JSON from `/api/v1{path}` with optional query string.
 * Throws {@link ApiError} for non-2xx responses (Problem Details when present).
 */
export async function fetchJson<T>(path: string, options: FetchJsonOptions = {}): Promise<T> {
  const url = buildUrl(path, options.query);
  const headers: Record<string, string> = { Accept: 'application/json' };
  if (options.accessToken) {
    headers.Authorization = `Bearer ${options.accessToken}`;
  }
  let response: Response;
  try {
    response = await fetch(url, {
      method: 'GET',
      headers,
      signal: options.signal,
    });
  } catch (err) {
    if (err instanceof Error && err.name === 'AbortError') {
      throw err;
    }
    throw new ApiError(0, 'Unable to reach QueenZone. Check your connection and try again.');
  }

  if (!response.ok) {
    const problem = await readProblem(response);
    throw new ApiError(
      response.status,
      messageFromProblem(response.status, problem, `Request failed (${response.status}).`),
      problem,
    );
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

/**
 * JSON write to `/api/v1{path}`. Sends `Authorization: Bearer` when
 * `accessToken` is present. Throws {@link ApiError} for non-2xx responses.
 */
export async function sendJson<T>(path: string, options: SendJsonOptions = {}): Promise<T> {
  const url = buildUrl(path, options.query);
  const headers: Record<string, string> = { Accept: 'application/json' };
  if (options.body !== undefined) {
    headers['Content-Type'] = 'application/json';
  }
  if (options.accessToken) {
    headers.Authorization = `Bearer ${options.accessToken}`;
  }

  let response: Response;
  try {
    response = await fetch(url, {
      method: options.method ?? 'POST',
      headers,
      body: options.body === undefined ? undefined : JSON.stringify(options.body),
      signal: options.signal,
    });
  } catch (err) {
    if (err instanceof Error && err.name === 'AbortError') {
      throw err;
    }
    throw new ApiError(0, 'Unable to reach QueenZone. Check your connection and try again.');
  }

  if (!response.ok) {
    const problem = await readProblem(response);
    throw new ApiError(
      response.status,
      messageFromProblem(response.status, problem, messageForWriteStatus(response.status)),
      problem,
    );
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

/**
 * Multipart write to `/api/v1{path}` (avatar upload, photo submissions).
 * Do not set Content-Type; fetch supplies the multipart boundary.
 */
export async function sendMultipart<T>(
  path: string,
  formData: FormData,
  options: FetchJsonOptions = {},
): Promise<T> {
  const url = buildUrl(path, options.query);
  const headers: Record<string, string> = { Accept: 'application/json' };
  if (options.accessToken) {
    headers.Authorization = `Bearer ${options.accessToken}`;
  }

  let response: Response;
  try {
    response = await fetch(url, {
      method: 'POST',
      headers,
      body: formData,
      signal: options.signal,
    });
  } catch (err) {
    if (err instanceof Error && err.name === 'AbortError') {
      throw err;
    }
    throw new ApiError(0, 'Unable to reach QueenZone. Check your connection and try again.');
  }

  if (!response.ok) {
    const problem = await readProblem(response);
    throw new ApiError(
      response.status,
      messageFromProblem(response.status, problem, messageForWriteStatus(response.status)),
      problem,
    );
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
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

export { formatPublishedDate, toPlainText } from './text';
