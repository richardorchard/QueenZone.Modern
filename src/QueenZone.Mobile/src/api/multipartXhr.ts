import { ApiError } from './errors';
import type { ProblemDetails } from './types';

export type MultipartXhrResult = {
  status: number;
  bodyText: string;
  contentType: string;
};

export type MultipartXhrLike = {
  status: number;
  responseText: string;
  timeout: number;
  responseType: string;
  open(method: string, url: string): void;
  setRequestHeader(name: string, value: string): void;
  getResponseHeader(name: string): string | null;
  send(body?: XMLHttpRequestBodyInit | null): void;
  abort(): void;
  onload: (() => void) | null;
  onerror: (() => void) | null;
  ontimeout: (() => void) | null;
  onabort: (() => void) | null;
};

export type TimedOutAbort = Error & { name: 'AbortError'; timedOut: true };

export function isTimedOutAbort(err: unknown): err is TimedOutAbort {
  return err instanceof Error && err.name === 'AbortError' && (err as { timedOut?: boolean }).timedOut === true;
}

export function classifyXhrFailure(err: unknown, caller?: AbortSignal): unknown {
  if (err instanceof ApiError) {
    return err;
  }

  if (caller?.aborted) {
    return err instanceof Error && err.name === 'AbortError'
      ? err
      : Object.assign(new Error('Aborted'), { name: 'AbortError' });
  }

  if (isTimedOutAbort(err)) {
    return ApiError.timeout(err);
  }

  if (err instanceof Error && err.name === 'AbortError') {
    return err;
  }

  return ApiError.offline(err);
}

export function postFormWithXhr(input: {
  url: string;
  formData: FormData;
  headers: Record<string, string>;
  timeoutMs: number;
  signal?: AbortSignal;
  xhrFactory?: () => MultipartXhrLike;
}): Promise<MultipartXhrResult> {
  return new Promise((resolve, reject) => {
    const xhr = input.xhrFactory ? input.xhrFactory() : new XMLHttpRequest();
    xhr.open('POST', input.url);
    xhr.timeout = input.timeoutMs;
    xhr.responseType = 'text';

    for (const [name, value] of Object.entries(input.headers)) {
      if (name.toLowerCase() === 'content-type') {
        continue;
      }
      xhr.setRequestHeader(name, value);
    }

    const cleanup = () => {
      input.signal?.removeEventListener('abort', onCallerAbort);
    };

    const onCallerAbort = () => {
      xhr.abort();
    };

    if (input.signal?.aborted) {
      reject(Object.assign(new Error('Aborted'), { name: 'AbortError' }));
      return;
    }

    input.signal?.addEventListener('abort', onCallerAbort);

    xhr.onload = () => {
      cleanup();
      resolve({
        status: xhr.status,
        bodyText: typeof xhr.responseText === 'string' ? xhr.responseText : '',
        contentType: xhr.getResponseHeader('content-type') ?? '',
      });
    };
    xhr.onerror = () => {
      cleanup();
      reject(new TypeError('Network request failed'));
    };
    xhr.ontimeout = () => {
      cleanup();
      reject(Object.assign(new Error('Timeout'), { name: 'AbortError', timedOut: true }));
    };
    xhr.onabort = () => {
      cleanup();
      reject(Object.assign(new Error('Aborted'), { name: 'AbortError' }));
    };

    try {
      xhr.send(input.formData);
    } catch (err) {
      cleanup();
      reject(err);
    }
  });
}

export function interpretMultipartXhrResult<T>(
  result: MultipartXhrResult,
  writeFallback: (status: number) => string,
  messageFromProblem: (status: number, problem: ProblemDetails | null, fallback: string) => string,
): T {
  if (result.status < 200 || result.status >= 300) {
    const problem = parseProblem(result);
    throw ApiError.http(
      result.status,
      messageFromProblem(result.status, problem, writeFallback(result.status)),
      problem,
    );
  }

  if (result.status === 204 || result.bodyText.trim() === '') {
    return undefined as T;
  }

  try {
    return JSON.parse(result.bodyText) as T;
  } catch {
    throw ApiError.malformed(result.status);
  }
}

function parseProblem(result: MultipartXhrResult): ProblemDetails | null {
  if (!result.contentType.toLowerCase().includes('json')) {
    return null;
  }

  try {
    return JSON.parse(result.bodyText) as ProblemDetails;
  } catch {
    return null;
  }
}
