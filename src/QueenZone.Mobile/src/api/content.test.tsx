import {
  fetchAlbumDetail,
  fetchArticleDetail,
  fetchArticlesPage,
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
  fetchTimelineEventById,
  fetchPhotoCategories,
  fetchPhotoCategory,
  fetchPhotoCategoryItems,
  fetchPhotoDetail,
  fetchHomePoll,
  fetchQuoteById,
  fetchRandomQuote,
  fetchRandomTrivia,
  voteHomePoll,
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

describe('fetchArticlesPage and fetchArticleDetail', () => {
  it('builds the list and article-detail URLs', async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse({ items: [] }));
    await fetchArticlesPage({ page: 2, pageSize: 20 });
    expect(lastUrl()).toBe('http://qz.test/api/v1/content/articles?page=2&pageSize=20');

    fetchMock.mockResolvedValueOnce(jsonResponse({ id: 101, title: 'Inside the Making of Bohemian Rhapsody' }));
    const detail = await fetchArticleDetail(101);
    expect(lastUrl()).toBe('http://qz.test/api/v1/content/articles/101');
    expect(detail).toMatchObject({ id: 101, title: 'Inside the Making of Bohemian Rhapsody' });
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

describe('fetchTimelineEventById', () => {
  it('requests a published event by id, including deep off-page ids', async () => {
    fetchMock.mockResolvedValueOnce(
      jsonResponse({
        id: 9999,
        title: 'Deep off-page event',
        summary: 'Would sit many pages down the chronological list.',
        eventDate: '1985-07-13T00:00:00Z',
        formattedDate: '13 July 1985',
        category: 'live',
        categoryLabel: 'Live',
        sourceUrl: null,
      }),
    );
    const event = await fetchTimelineEventById(9999);
    expect(lastUrl()).toBe('http://qz.test/api/v1/content/timeline/9999');
    expect(event).toMatchObject({ id: 9999, title: 'Deep off-page event' });
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

describe('fetchRandomTrivia', () => {
  it('requests the random trivia fact and returns null when none are published', async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse(null));
    const fact = await fetchRandomTrivia();
    expect(lastUrl()).toBe('http://qz.test/api/v1/content/trivia/random');
    expect(fact).toBeNull();
  });

  it('returns the parsed trivia body', async () => {
    fetchMock.mockResolvedValueOnce(
      jsonResponse({
        id: 12,
        text: 'The first Queen album was recorded in 1972.',
        category: 'Studio',
        difficulty: 'Easy',
        source: 'Queen archive',
      }),
    );
    const fact = await fetchRandomTrivia();
    expect(fact).toMatchObject({
      id: 12,
      text: 'The first Queen album was recorded in 1972.',
      category: 'Studio',
      difficulty: 'Easy',
      source: 'Queen archive',
    });
  });
});

describe('fetchHomePoll', () => {
  it('requests the current home poll and returns null when none is live', async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse(null));
    const poll = await fetchHomePoll();
    expect(lastUrl()).toBe('http://qz.test/api/v1/content/home-poll');
    expect(poll).toBeNull();
  });

  it('forwards a bearer token so the viewer choice can be marked', async () => {
    fetchMock.mockResolvedValueOnce(
      jsonResponse({
        id: 'poll-1',
        question: 'Best album?',
        options: [],
        totalVotes: 0,
        isClosed: false,
        viewerHasVoted: false,
        selectedOptionId: null,
      }),
    );
    await fetchHomePoll(undefined, 'member-token');
    const init = fetchMock.mock.calls.at(-1)?.[1] as RequestInit;
    expect((init.headers as Record<string, string>).Authorization).toBe('Bearer member-token');
  });
});

describe('voteHomePoll', () => {
  it('posts the option id to the home-poll votes path', async () => {
    fetchMock.mockResolvedValueOnce(
      jsonResponse({
        id: 'poll-1',
        question: 'Best album?',
        options: [],
        totalVotes: 1,
        isClosed: false,
        viewerHasVoted: true,
        selectedOptionId: 'opt-1',
      }),
    );
    await voteHomePoll('opt-1', 'member-token');
    expect(lastUrl()).toBe('http://qz.test/api/v1/content/home-poll/votes');
    const init = fetchMock.mock.calls.at(-1)?.[1] as RequestInit;
    expect(init.method).toBe('POST');
    expect(init.body).toBe(JSON.stringify({ optionId: 'opt-1' }));
    expect((init.headers as Record<string, string>).Authorization).toBe('Bearer member-token');
  });
});

describe('fetchQuoteById', () => {
  it('requests the published quote by id', async () => {
    fetchMock.mockResolvedValueOnce(
      jsonResponse({ id: 5, text: 'A kind of magic', whoSaid: 'Freddie Mercury', context: 'Live Aid' }),
    );
    const quote = await fetchQuoteById(5);
    expect(lastUrl()).toBe('http://qz.test/api/v1/content/quotes/5');
    expect(quote).toMatchObject({
      id: 5,
      text: 'A kind of magic',
      whoSaid: 'Freddie Mercury',
      context: 'Live Aid',
    });
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
