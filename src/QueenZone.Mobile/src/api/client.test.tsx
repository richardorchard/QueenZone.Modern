import { ApiError, fetchJson, sendJson, sendMultipart } from './client';
import { jsonResponse } from '../test/fixtures';

jest.mock('../config', () => ({
  apiV1Url: (path: string) => `http://qz.test/api/v1${path.startsWith('/') ? path : `/${path}`}`,
}));

const fetchMock = jest.fn<Promise<Response>, [RequestInfo | URL, RequestInit?]>();

beforeEach(() => {
  fetchMock.mockReset();
  global.fetch = fetchMock as unknown as typeof fetch;
});

function lastCall() {
  const call = fetchMock.mock.calls.at(-1);
  if (!call) {
    throw new Error('fetch was not called');
  }
  return { url: String(call[0]), init: call[1] ?? {} };
}

describe('fetchJson', () => {
  it('builds query URLs and sends a Bearer token', async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse({ ok: true }));
    await fetchJson('/content/news', { query: { page: 2, pageSize: 20, empty: '' }, accessToken: 'tok' });
    const { url, init } = lastCall();
    expect(url).toBe('http://qz.test/api/v1/content/news?page=2&pageSize=20');
    expect(init.method).toBe('GET');
    expect(init.headers).toEqual({
      Accept: 'application/json',
      Authorization: 'Bearer tok',
    });
  });

  it('returns undefined for 204', async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse(null, 204));
    await expect(fetchJson('/health')).resolves.toBeUndefined();
  });

  it('prefers RFC 7807 detail over title', async () => {
    fetchMock.mockResolvedValueOnce(
      jsonResponse({ title: 'Conflict', detail: 'Already exists.' }, 409),
    );
    await expect(fetchJson('/dup')).rejects.toMatchObject({
      name: 'ApiError',
      status: 409,
      message: 'Already exists.',
    });
  });

  it('uses title when detail is blank', async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse({ title: 'Gone', detail: '  ' }, 410));
    await expect(fetchJson('/gone')).rejects.toMatchObject({ message: 'Gone' });
  });

  it('maps 401, 403, 404, 5xx, and non-JSON bodies', async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse({}, 401));
    await expect(fetchJson('/me')).rejects.toMatchObject({ status: 401, message: 'Request failed (401).' });

    fetchMock.mockResolvedValueOnce(jsonResponse({}, 403));
    await expect(fetchJson('/me')).rejects.toMatchObject({ status: 403, message: 'Request failed (403).' });

    fetchMock.mockResolvedValueOnce(new Response('nope', { status: 404, headers: { 'Content-Type': 'text/plain' } }));
    await expect(fetchJson('/missing')).rejects.toMatchObject({ status: 404, message: 'Not found.' });

    fetchMock.mockResolvedValueOnce(jsonResponse({}, 503));
    await expect(fetchJson('/down')).rejects.toMatchObject({
      message: 'The server had a problem. Try again shortly.',
    });

    fetchMock.mockResolvedValueOnce(
      new Response('{not-json', { status: 400, headers: { 'Content-Type': 'application/json' } }),
    );
    await expect(fetchJson('/bad')).rejects.toBeInstanceOf(ApiError);
  });

  it('rethrows AbortError and maps network failures', async () => {
    const abort = Object.assign(new Error('aborted'), { name: 'AbortError' });
    fetchMock.mockRejectedValueOnce(abort);
    await expect(fetchJson('/x', { signal: AbortSignal.abort() })).rejects.toBe(abort);

    fetchMock.mockRejectedValueOnce(new TypeError('Failed to fetch'));
    await expect(fetchJson('/x')).rejects.toMatchObject({
      status: 0,
      message: 'Unable to reach QueenZone. Check your connection and try again.',
    });
  });
});

describe('sendJson and sendMultipart', () => {
  it('sends JSON writes with content-type and Bearer', async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse({ id: 1 }));
    await sendJson('/member/notes', { body: { text: 'hi' }, accessToken: 'tok', method: 'POST' });
    const { init } = lastCall();
    expect(init.method).toBe('POST');
    expect(init.body).toBe(JSON.stringify({ text: 'hi' }));
    expect(init.headers).toEqual({
      Accept: 'application/json',
      'Content-Type': 'application/json',
      Authorization: 'Bearer tok',
    });
  });

  it('does not set Content-Type for multipart', async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse({ id: 9 }));
    const form = new FormData();
    form.append('photo', 'blob');
    await sendMultipart('/member/photo-submissions', form, { accessToken: 'tok' });
    const { init } = lastCall();
    expect(init.method).toBe('POST');
    expect(init.body).toBe(form);
    expect(init.headers).toEqual({
      Accept: 'application/json',
      Authorization: 'Bearer tok',
    });
  });

  it('maps write status copy for 401, 403, 409, and 429', async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse({}, 401));
    await expect(sendJson('/x', { body: {} })).rejects.toMatchObject({ message: 'Sign in to continue.' });

    fetchMock.mockResolvedValueOnce(jsonResponse({}, 403));
    await expect(sendJson('/x', { body: {} })).rejects.toMatchObject({ message: 'You cannot do that.' });

    fetchMock.mockResolvedValueOnce(jsonResponse({}, 409));
    await expect(sendJson('/x', { body: {} })).rejects.toMatchObject({ message: 'Request failed (409).' });

    fetchMock.mockResolvedValueOnce(jsonResponse({}, 429));
    await expect(sendJson('/x', { body: {} })).rejects.toMatchObject({
      message: "You're posting too quickly. Please wait a bit and try again.",
    });
  });

  it('propagates AbortError on writes', async () => {
    const abort = Object.assign(new Error('aborted'), { name: 'AbortError' });
    fetchMock.mockRejectedValueOnce(abort);
    await expect(sendJson('/x', { body: {} })).rejects.toBe(abort);
    fetchMock.mockRejectedValueOnce(abort);
    await expect(sendMultipart('/x', new FormData())).rejects.toBe(abort);
  });
});
