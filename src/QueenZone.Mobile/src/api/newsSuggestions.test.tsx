import { createNewsSuggestion } from './newsSuggestions';
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

const created = {
  id: '11111111-1111-1111-1111-111111111111',
  status: 'Pending',
  url: 'https://www.bbc.co.uk/news/example',
  title: 'Queen announce dates',
  submittedAt: '2026-08-26T10:00:00Z',
};

describe('createNewsSuggestion', () => {
  it('posts JSON with a Bearer token and parses 201', async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse(created, 201));
    const result = await createNewsSuggestion(
      { url: created.url, title: created.title, notes: null },
      'tok',
    );
    const { url, init } = lastCall();
    expect(url).toBe('http://qz.test/api/v1/member/news-suggestions');
    expect(init.method).toBe('POST');
    expect(init.headers).toMatchObject({
      Authorization: 'Bearer tok',
      'Content-Type': 'application/json',
    });
    expect(result).toEqual(created);
  });

  it('maps 400, 409, 429, 401, and network failures through ApiError', async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse({ title: 'Bad Request', detail: 'Use https' }, 400));
    await expect(
      createNewsSuggestion({ url: 'http://example.com', title: null, notes: null }, 'tok'),
    ).rejects.toMatchObject({ name: 'ApiError', status: 400, message: 'Use https' });

    fetchMock.mockResolvedValueOnce(jsonResponse({ title: 'Conflict', detail: 'Already suggested' }, 409));
    await expect(
      createNewsSuggestion({ url: created.url, title: null, notes: null }, 'tok'),
    ).rejects.toMatchObject({ name: 'ApiError', status: 409, message: 'Already suggested' });

    fetchMock.mockResolvedValueOnce(jsonResponse({ title: 'Too Many Requests' }, 429));
    await expect(
      createNewsSuggestion({ url: created.url, title: null, notes: null }, 'tok'),
    ).rejects.toMatchObject({ name: 'ApiError', status: 429 });

    fetchMock.mockResolvedValueOnce(jsonResponse({ title: 'Unauthorized' }, 401));
    await expect(
      createNewsSuggestion({ url: created.url, title: null, notes: null }, 'tok'),
    ).rejects.toMatchObject({ name: 'ApiError', status: 401 });

    fetchMock.mockRejectedValueOnce(new TypeError('Failed to fetch'));
    await expect(
      createNewsSuggestion({ url: created.url, title: null, notes: null }, 'tok'),
    ).rejects.toMatchObject({ name: 'ApiError', status: 0 });
  });
});
