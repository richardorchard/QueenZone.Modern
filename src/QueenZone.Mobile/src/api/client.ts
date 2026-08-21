import { apiV1Url } from '../config';
import { ApiError } from './errors';
import type { ProblemDetails } from './types';

export { ApiError } from './errors';

export type FetchJsonOptions = {
  signal?: AbortSignal;
  query?: Record<string, string | number | undefined | null>;
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
  let response: Response;
  try {
    response = await fetch(url, {
      method: 'GET',
      headers: { Accept: 'application/json' },
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

export { formatPublishedDate, toPlainText } from './text';
