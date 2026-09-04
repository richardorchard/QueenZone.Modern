import { fetchJsonWithOfflineCache } from '../cache';
import { fetchJson, sendJson } from './client';
import type {
  AlbumDetail,
  AlbumListItem,
  ApiPagedResponse,
  ArticleDetail,
  ArticleListItem,
  BiographyChapterDetail,
  BiographyChapterListItem,
  FreddieTribute,
  HomePoll,
  LiveActivitySummary,
  NewsDetail,
  NewsListItem,
  NewsYearRange,
  PhotoCategoryListItem,
  PhotoDetail,
  PhotoListItem,
  FanPerformance,
  RandomQuote,
  RandomTrivia,
  TimelineEvent,
} from './types';

export type PageQuery = {
  page?: number;
  pageSize?: number;
  signal?: AbortSignal;
};

function pageParams({ page, pageSize }: PageQuery) {
  return {
    page,
    pageSize,
  };
}

export type NewsPageQuery = PageQuery & {
  /** First year of a 10-year span (e.g. 2010 for the 2010s). Server-side filter — see issue #838. */
  decade?: number;
  /** A single year (e.g. 2008). Server-side filter for the year-rail scrubber — see issue #886. Wins over `decade` if both are set. */
  year?: number;
};

export function fetchNewsPage(query: NewsPageQuery = {}): Promise<ApiPagedResponse<NewsListItem>> {
  return fetchJson('/content/news', {
    query: { ...pageParams(query), decade: query.decade, year: query.year },
    signal: query.signal,
  });
}

/** Earliest/latest published years in the archive, for the year-rail scrubber's tick marks. */
export function fetchNewsYearRange(signal?: AbortSignal): Promise<NewsYearRange> {
  return fetchJson('/content/news/years', { signal });
}

/** Network-first; caches successful responses for offline re-open. */
export function fetchNewsDetail(id: number, signal?: AbortSignal): Promise<NewsDetail> {
  return fetchJsonWithOfflineCache(`/content/news/${id}`, {
    signal,
    cacheKey: `news:${id}`,
  });
}

export function fetchArticlesPage(
  query: PageQuery = {},
): Promise<ApiPagedResponse<ArticleListItem>> {
  return fetchJson('/content/articles', { query: pageParams(query), signal: query.signal });
}

/** Network-first; caches successful responses for offline re-open. */
export function fetchArticleDetail(id: number, signal?: AbortSignal): Promise<ArticleDetail> {
  return fetchJsonWithOfflineCache(`/content/articles/${id}`, {
    signal,
    cacheKey: `articles:${id}`,
  });
}

export function fetchBiographyPage(
  query: PageQuery = {},
): Promise<ApiPagedResponse<BiographyChapterListItem>> {
  return fetchJson('/content/biography', { query: pageParams(query), signal: query.signal });
}

/** Network-first; caches successful responses for offline re-open. */
export function fetchBiographyChapter(
  id: number,
  signal?: AbortSignal,
): Promise<BiographyChapterDetail> {
  return fetchJsonWithOfflineCache(`/content/biography/${id}`, {
    signal,
    cacheKey: `biography:${id}`,
  });
}

export function fetchDiscographyPage(
  query: PageQuery = {},
): Promise<ApiPagedResponse<AlbumListItem>> {
  return fetchJson('/content/discography', { query: pageParams(query), signal: query.signal });
}

/** Network-first; caches successful responses for offline re-open. */
export function fetchAlbumDetail(id: number, signal?: AbortSignal): Promise<AlbumDetail> {
  return fetchJsonWithOfflineCache(`/content/discography/${id}`, {
    signal,
    cacheKey: `discography:${id}`,
  });
}

export function fetchTimelinePage(
  query: PageQuery = {},
): Promise<ApiPagedResponse<TimelineEvent>> {
  return fetchJson('/content/timeline', { query: pageParams(query), signal: query.signal });
}

/** A published timeline event by id. 404 when missing or unpublished. */
export function fetchTimelineEventById(id: number, signal?: AbortSignal): Promise<TimelineEvent> {
  return fetchJson(`/content/timeline/${id}`, { signal });
}

/** The single most notable history event for today's date, or null when there is none. */
export function fetchOnThisDay(signal?: AbortSignal): Promise<TimelineEvent | null> {
  return fetchJson('/content/on-this-day', { signal });
}

/**
 * Count of new forum replies posted today. No presence/reading tracking exists, so this is
 * the only honest live signal for the home screen's activity strip.
 */
export function fetchLiveActivity(signal?: AbortSignal): Promise<LiveActivitySummary> {
  return fetchJson('/content/live-activity', { signal });
}

/** A single random published quote, or null when none are published. */
export function fetchRandomQuote(signal?: AbortSignal): Promise<RandomQuote | null> {
  return fetchJson('/content/quotes/random', { signal });
}

/** A published quote by id. 404 when missing or unpublished. */
export function fetchQuoteById(id: number, signal?: AbortSignal): Promise<RandomQuote> {
  return fetchJson(`/content/quotes/${id}`, { signal });
}

/** A single random published trivia fact, or null when none are published. */
export function fetchRandomTrivia(signal?: AbortSignal): Promise<RandomTrivia | null> {
  return fetchJson('/content/trivia/random', { signal });
}

/** The current Home poll, or null when none is live. Optional Bearer marks the viewer's choice. */
export function fetchHomePoll(
  signal?: AbortSignal,
  accessToken?: string | null,
): Promise<HomePoll | null> {
  return fetchJson('/content/home-poll', { signal, accessToken });
}

/** Cast one ballot. Caller must refetch `fetchHomePoll` — do not optimistic-increment. */
export function voteHomePoll(
  optionId: string,
  accessToken: string,
  signal?: AbortSignal,
): Promise<HomePoll> {
  return sendJson('/content/home-poll/votes', {
    method: 'POST',
    body: { optionId },
    accessToken,
    signal,
  });
}

export function fetchFreddieTributePage(
  query: PageQuery = {},
): Promise<ApiPagedResponse<FreddieTribute>> {
  return fetchJson('/content/freddietribute', { query: pageParams(query), signal: query.signal });
}

export function fetchFanPerformancesPage(
  query: PageQuery = {},
): Promise<ApiPagedResponse<FanPerformance>> {
  return fetchJson('/content/fan-performances', { query: pageParams(query), signal: query.signal });
}

/** Max page size the JSON API accepts (`ApiPagination.MaxPageSize`). */
const FAN_PERFORMANCE_CATALOG_PAGE_SIZE = 100;

/**
 * Full published catalog for Play All / Shuffle Play All.
 * Walks existing paged list GETs; does not use the list screen's loaded pages.
 */
export async function fetchAllFanPerformances(signal?: AbortSignal): Promise<FanPerformance[]> {
  const catalog: FanPerformance[] = [];
  let page = 1;
  let totalPages = 1;

  do {
    const response = await fetchFanPerformancesPage({
      page,
      pageSize: FAN_PERFORMANCE_CATALOG_PAGE_SIZE,
      signal,
    });
    catalog.push(...response.items);
    totalPages = Math.max(response.totalPages, 1);
    page += 1;
  } while (page <= totalPages);

  return catalog;
}

export function fetchFanPerformanceDetail(
  id: number,
  signal?: AbortSignal,
): Promise<FanPerformance> {
  return fetchJson(`/content/fan-performances/${id}`, { signal });
}

export type PhotoPageQuery = PageQuery & {
  size?: string;
};

export function fetchPhotoCategories(
  query: PageQuery = {},
): Promise<ApiPagedResponse<PhotoCategoryListItem>> {
  return fetchJson('/content/photos/categories', { query: pageParams(query), signal: query.signal });
}

export function fetchPhotoCategory(
  slug: string,
  signal?: AbortSignal,
): Promise<PhotoCategoryListItem> {
  return fetchJson(`/content/photos/categories/${encodeURIComponent(slug)}`, { signal });
}

export function fetchPhotoCategoryItems(
  slug: string,
  query: PhotoPageQuery = {},
): Promise<ApiPagedResponse<PhotoListItem>> {
  return fetchJson(`/content/photos/categories/${encodeURIComponent(slug)}/items`, {
    query: { ...pageParams(query), size: query.size },
    signal: query.signal,
  });
}

export function fetchPhotoDetail(
  slug: string,
  picId: number,
  query: { size?: string; signal?: AbortSignal } = {},
): Promise<PhotoDetail> {
  return fetchJson(`/content/photos/categories/${encodeURIComponent(slug)}/items/${picId}`, {
    query: { size: query.size },
    signal: query.signal,
  });
}
