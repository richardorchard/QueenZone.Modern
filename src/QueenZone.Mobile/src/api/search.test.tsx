import { fetchSearchPage } from './search';
import { jsonResponse } from '../test/fixtures';

jest.mock('../config', () => ({
  apiV1Url: (path: string) => `http://qz.test/api/v1${path.startsWith('/') ? path : `/${path}`}`,
}));

const fetchMock = jest.fn<Promise<Response>, [RequestInfo | URL, RequestInit?]>();

beforeEach(() => {
  fetchMock.mockReset();
  global.fetch = fetchMock as unknown as typeof fetch;
});

function lastUrl() {
  const call = fetchMock.mock.calls.at(-1);
  if (!call) {
    throw new Error('fetch was not called');
  }
  return String(call[0]);
}

describe('fetchSearchPage', () => {
  it('sends the query, type, and page params', async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse({ items: [] }));
    await fetchSearchPage({ q: 'freddie', type: 'news', page: 2, pageSize: 10 });
    expect(lastUrl()).toBe(
      'http://qz.test/api/v1/search?q=freddie&type=news&page=2&pageSize=10',
    );
  });

  it('omits type when not given, even for an empty query', async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse({ items: [] }));
    await fetchSearchPage({ q: '' });
    expect(lastUrl()).toBe('http://qz.test/api/v1/search');
  });
});
