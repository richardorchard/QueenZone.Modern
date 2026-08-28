import {
  fetchAlbumDetail,
  fetchBiographyChapter,
  fetchBiographyPage,
  fetchDiscographyPage,
  fetchAllFanPerformances,
  fetchFanPerformanceDetail,
  fetchFanPerformancesPage,
  fetchFreddieTributePage,
  fetchLiveActivity,
  fetchNewsDetail,
  fetchNewsPage,
  fetchOnThisDay,
  fetchPhotoCategories,
  fetchPhotoCategory,
  fetchPhotoCategoryItems,
  fetchPhotoDetail,
  fetchRandomQuote,
  fetchTimelinePage,
} from './content';
import { fanPerformanceFixture, jsonResponse } from '../test/fixtures';

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

describe('fetchNewsPage', () => {
  it('paginates and forwards the decade filter', async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse({ items: [] }));
    await fetchNewsPage({ page: 2, pageSize: 10, decade: 2010 });
    expect(lastUrl()).toBe('http://qz.test/api/v1/content/news?page=2&pageSize=10&decade=2010');
  });

  it('omits decade when not given', async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse({ items: [] }));
    await fetchNewsPage();
    expect(lastUrl()).toBe('http://qz.test/api/v1/content/news');
  });
});

describe('fetchNewsDetail', () => {
  it('requests the numeric id path and returns the parsed body', async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse({ id: 42, title: 'Story' }));
    const detail = await fetchNewsDetail(42);
    expect(lastUrl()).toBe('http://qz.test/api/v1/content/news/42');
    expect(detail).toMatchObject({ id: 42, title: 'Story' });
  });
});

describe('fetchBiographyPage and fetchBiographyChapter', () => {
  it('builds the list and chapter-detail URLs', async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse({ items: [] }));
    await fetchBiographyPage({ page: 3 });
    expect(lastUrl()).toBe('http://qz.test/api/v1/content/biography?page=3');

    fetchMock.mockResolvedValueOnce(jsonResponse({ id: 7 }));
    await fetchBiographyChapter(7);
    expect(lastUrl()).toBe('http://qz.test/api/v1/content/biography/7');
  });
});

describe('fetchDiscographyPage and fetchAlbumDetail', () => {
  it('builds the list and album-detail URLs', async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse({ items: [] }));
    await fetchDiscographyPage();
    expect(lastUrl()).toBe('http://qz.test/api/v1/content/discography');

    fetchMock.mockResolvedValueOnce(jsonResponse({ albumId: 5 }));
    await fetchAlbumDetail(5);
    expect(lastUrl()).toBe('http://qz.test/api/v1/content/discography/5');
  });
});

describe('fetchTimelinePage and fetchOnThisDay', () => {
  it('requests the timeline list and the single on-this-day event', async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse({ items: [] }));
    await fetchTimelinePage({ page: 1, pageSize: 5 });
    expect(lastUrl()).toBe('http://qz.test/api/v1/content/timeline?page=1&pageSize=5');

    fetchMock.mockResolvedValueOnce(jsonResponse(null));
    const event = await fetchOnThisDay();
    expect(lastUrl()).toBe('http://qz.test/api/v1/content/on-this-day');
    expect(event).toBeNull();
  });
});

describe('fetchLiveActivity', () => {
  it('requests the live-activity summary', async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse({ newForumRepliesToday: 3 }));
    const summary = await fetchLiveActivity();
    expect(lastUrl()).toBe('http://qz.test/api/v1/content/live-activity');
    expect(summary.newForumRepliesToday).toBe(3);
  });
});

describe('fetchRandomQuote', () => {
  it('requests the random quote and returns null when none are published', async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse(null));
    const quote = await fetchRandomQuote();
    expect(lastUrl()).toBe('http://qz.test/api/v1/content/quotes/random');
    expect(quote).toBeNull();
  });

  it('returns the parsed quote body', async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse({ id: 5, text: 'A kind of magic', whoSaid: 'Freddie Mercury' }));
    const quote = await fetchRandomQuote();
    expect(quote).toMatchObject({ id: 5, text: 'A kind of magic', whoSaid: 'Freddie Mercury' });
  });
});

describe('fetchFreddieTributePage and fetchFanPerformancesPage/Detail', () => {
  it('builds tribute and fan-performance URLs', async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse({ items: [] }));
    await fetchFreddieTributePage({ page: 1 });
    expect(lastUrl()).toBe('http://qz.test/api/v1/content/freddietribute?page=1');

    fetchMock.mockResolvedValueOnce(jsonResponse({ items: [] }));
    await fetchFanPerformancesPage();
    expect(lastUrl()).toBe('http://qz.test/api/v1/content/fan-performances');

    fetchMock.mockResolvedValueOnce(jsonResponse({ id: 9 }));
    await fetchFanPerformanceDetail(9);
    expect(lastUrl()).toBe('http://qz.test/api/v1/content/fan-performances/9');
  });

  it('fetchAllFanPerformances walks every page at pageSize 100', async () => {
    const first = fanPerformanceFixture({ id: 1, title: 'First' });
    const second = fanPerformanceFixture({ id: 2, title: 'Second' });
    fetchMock
      .mockResolvedValueOnce(
        jsonResponse({
          items: [first],
          page: 1,
          pageSize: 100,
          totalCount: 2,
          totalPages: 2,
        }),
      )
      .mockResolvedValueOnce(
        jsonResponse({
          items: [second],
          page: 2,
          pageSize: 100,
          totalCount: 2,
          totalPages: 2,
        }),
      );

    const catalog = await fetchAllFanPerformances();
    expect(catalog).toEqual([first, second]);
    expect(fetchMock).toHaveBeenCalledTimes(2);
    expect(String(fetchMock.mock.calls[0]?.[0])).toBe(
      'http://qz.test/api/v1/content/fan-performances?page=1&pageSize=100',
    );
    expect(String(fetchMock.mock.calls[1]?.[0])).toBe(
      'http://qz.test/api/v1/content/fan-performances?page=2&pageSize=100',
    );
  });
});

describe('photo content endpoints', () => {
  it('builds category list, single-category, item-list, and detail URLs', async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse({ items: [] }));
    await fetchPhotoCategories({ page: 2 });
    expect(lastUrl()).toBe('http://qz.test/api/v1/content/photos/categories?page=2');

    fetchMock.mockResolvedValueOnce(jsonResponse({ catId: 1 }));
    await fetchPhotoCategory('live shots');
    expect(lastUrl()).toBe('http://qz.test/api/v1/content/photos/categories/live%20shots');

    fetchMock.mockResolvedValueOnce(jsonResponse({ items: [] }));
    await fetchPhotoCategoryItems('live-shots', { size: 'thumb' });
    expect(lastUrl()).toBe(
      'http://qz.test/api/v1/content/photos/categories/live-shots/items?size=thumb',
    );

    fetchMock.mockResolvedValueOnce(jsonResponse({ picId: 3 }));
    await fetchPhotoDetail('live-shots', 3, { size: 'full' });
    expect(lastUrl()).toBe(
      'http://qz.test/api/v1/content/photos/categories/live-shots/items/3?size=full',
    );
  });
});
