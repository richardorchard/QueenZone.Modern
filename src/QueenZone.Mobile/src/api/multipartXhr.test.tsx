import { ApiError } from './errors';
import {
  classifyXhrFailure,
  interpretMultipartXhrResult,
  postFormWithXhr,
  type MultipartXhrLike,
} from './multipartXhr';

function messageFromProblem(status: number, problem: { detail?: string } | null, fallback: string): string {
  return problem?.detail?.trim() || fallback;
}

function writeFallback(status: number): string {
  if (status === 401) {
    return 'Sign in to continue.';
  }
  return `Request failed (${status}).`;
}

class FakeXhr implements MultipartXhrLike {
  status = 200;
  responseText = '{"ok":true}';
  timeout = 0;
  responseType = '';
  contentType = 'application/json';
  headers: Record<string, string> = {};
  sent: FormData | null = null;
  aborted = false;
  failSend: Error | null = null;
  mode: 'load' | 'error' | 'timeout' | 'abort' | 'hang' = 'load';
  onload: (() => void) | null = null;
  onerror: (() => void) | null = null;
  ontimeout: (() => void) | null = null;
  onabort: (() => void) | null = null;

  open(): void {}

  setRequestHeader(name: string, value: string): void {
    this.headers[name] = value;
  }

  getResponseHeader(name: string): string | null {
    return name.toLowerCase() === 'content-type' ? this.contentType : null;
  }

  send(body?: XMLHttpRequestBodyInit | null): void {
    if (this.failSend) {
      throw this.failSend;
    }
    this.sent = body as FormData;
    if (this.mode === 'hang') {
      return;
    }
    queueMicrotask(() => {
      if (this.mode === 'load') {
        this.onload?.();
      } else if (this.mode === 'error') {
        this.onerror?.();
      } else if (this.mode === 'timeout') {
        this.ontimeout?.();
      } else if (this.mode === 'abort') {
        this.onabort?.();
      }
    });
  }

  abort(): void {
    this.aborted = true;
    this.onabort?.();
  }
}

describe('postFormWithXhr', () => {
  it('posts the form without a Content-Type header', async () => {
    const xhr = new FakeXhr();
    const form = new FormData();
    form.append('file', 'avatar');
    const result = await postFormWithXhr({
      url: 'http://qz.test/api/v1/me/avatar',
      formData: form,
      headers: {
        Accept: 'application/json',
        Authorization: 'Bearer tok',
        'Content-Type': 'multipart/form-data',
      },
      timeoutMs: 1_000,
      xhrFactory: () => xhr,
    });

    expect(xhr.timeout).toBe(1_000);
    expect(xhr.headers).toEqual({
      Accept: 'application/json',
      Authorization: 'Bearer tok',
    });
    expect(xhr.sent).toBe(form);
    expect(result).toEqual({
      status: 200,
      bodyText: '{"ok":true}',
      contentType: 'application/json',
    });
  });

  it('maps network, timeout, abort, and send failures', async () => {
    const network = new FakeXhr();
    network.mode = 'error';
    await expect(
      postFormWithXhr({
        url: 'http://qz.test/x',
        formData: new FormData(),
        headers: {},
        timeoutMs: 10,
        xhrFactory: () => network,
      }),
    ).rejects.toBeInstanceOf(TypeError);

    const timeout = new FakeXhr();
    timeout.mode = 'timeout';
    await expect(
      postFormWithXhr({
        url: 'http://qz.test/x',
        formData: new FormData(),
        headers: {},
        timeoutMs: 10,
        xhrFactory: () => timeout,
      }),
    ).rejects.toMatchObject({ name: 'AbortError', timedOut: true });

    const abort = new FakeXhr();
    abort.mode = 'abort';
    await expect(
      postFormWithXhr({
        url: 'http://qz.test/x',
        formData: new FormData(),
        headers: {},
        timeoutMs: 10,
        xhrFactory: () => abort,
      }),
    ).rejects.toMatchObject({ name: 'AbortError' });

    const sendFail = new FakeXhr();
    sendFail.failSend = new Error('cannot serialize');
    await expect(
      postFormWithXhr({
        url: 'http://qz.test/x',
        formData: new FormData(),
        headers: {},
        timeoutMs: 10,
        xhrFactory: () => sendFail,
      }),
    ).rejects.toThrow('cannot serialize');
  });

  it('aborts when the caller signal is already aborted', async () => {
    await expect(
      postFormWithXhr({
        url: 'http://qz.test/x',
        formData: new FormData(),
        headers: {},
        timeoutMs: 10,
        signal: AbortSignal.abort(),
        xhrFactory: () => new FakeXhr(),
      }),
    ).rejects.toMatchObject({ name: 'AbortError' });
  });

  it('aborts an in-flight request when the caller cancels', async () => {
    const xhr = new FakeXhr();
    xhr.mode = 'hang';
    const controller = new AbortController();
    const pending = postFormWithXhr({
      url: 'http://qz.test/x',
      formData: new FormData(),
      headers: {},
      timeoutMs: 10_000,
      signal: controller.signal,
      xhrFactory: () => xhr,
    });
    controller.abort();
    await expect(pending).rejects.toMatchObject({ name: 'AbortError' });
    expect(xhr.aborted).toBe(true);
  });
});

describe('interpretMultipartXhrResult', () => {
  it('parses JSON, empty 204, problems, and malformed bodies', () => {
    expect(
      interpretMultipartXhrResult(
        { status: 200, bodyText: '{"id":9}', contentType: 'application/json' },
        writeFallback,
        messageFromProblem,
      ),
    ).toEqual({ id: 9 });

    expect(
      interpretMultipartXhrResult(
        { status: 204, bodyText: '', contentType: '' },
        writeFallback,
        messageFromProblem,
      ),
    ).toBeUndefined();

    expect(() =>
      interpretMultipartXhrResult(
        { status: 401, bodyText: '{"detail":"Sign in."}', contentType: 'application/json' },
        writeFallback,
        messageFromProblem,
      ),
    ).toThrow(ApiError);
    try {
      interpretMultipartXhrResult(
        { status: 401, bodyText: '{"detail":"Sign in."}', contentType: 'application/json' },
        writeFallback,
        messageFromProblem,
      );
    } catch (err) {
      expect(err).toMatchObject({ kind: 'http', status: 401, message: 'Sign in.' });
    }

    try {
      interpretMultipartXhrResult(
        { status: 200, bodyText: '{nope', contentType: 'application/json' },
        writeFallback,
        messageFromProblem,
      );
      throw new Error('expected malformed');
    } catch (err) {
      expect(err).toMatchObject({ kind: 'malformed', status: 200 });
    }

    try {
      interpretMultipartXhrResult(
        { status: 400, bodyText: '{nope', contentType: 'application/json' },
        writeFallback,
        messageFromProblem,
      );
      throw new Error('expected http');
    } catch (err) {
      expect(err).toMatchObject({ kind: 'http', status: 400, message: 'Request failed (400).' });
    }
  });
});

describe('classifyXhrFailure', () => {
  it('maps timeout, abort, and network errors', () => {
    const http = ApiError.http(401, 'Sign in to continue.');
    expect(classifyXhrFailure(http)).toBe(http);

    const timeout = Object.assign(new Error('Timeout'), { name: 'AbortError', timedOut: true as const });
    expect(classifyXhrFailure(timeout)).toMatchObject({ kind: 'timeout' });

    const abort = Object.assign(new Error('Aborted'), { name: 'AbortError' });
    expect(classifyXhrFailure(abort)).toBe(abort);

    const offline = new TypeError('Network request failed');
    expect(classifyXhrFailure(offline)).toBeInstanceOf(ApiError);
    expect(classifyXhrFailure(offline)).toMatchObject({ kind: 'offline', cause: offline });

    const caller = AbortSignal.abort();
    expect(classifyXhrFailure(new TypeError('x'), caller)).toMatchObject({ name: 'AbortError' });
    expect(classifyXhrFailure(abort, caller)).toBe(abort);
  });
});
