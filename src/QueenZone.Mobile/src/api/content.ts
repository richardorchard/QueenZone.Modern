import { fetchJsonWithOfflineCache } from '../cache';
import { fetchJson } from './client';
import type {
  AlbumDetail,
  AlbumListItem,
  ApiPagedResponse,
  BiographyChapterDetail,
  BiographyChapterListItem,
  FreddieTribute,
  LiveActivitySummary,
  NewsDetail,
  NewsListItem,
  PhotoCategoryListItem,
  PhotoDetail,
  PhotoListItem,
  FanPerformance,
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
};

export function fetchNewsPage(query: NewsPageQuery = {}): Promise<ApiPagedResponse<NewsListItem>> {
  return fetchJson('/content/news', {
    query: { ...pageParams(query), decade: query.decade },
    signal: query.signal,
  });
}

/** Network-first; caches successful responses for offline re-open. */
export function fetchNewsDetail(id: number, signal?: AbortSignal): Promise<NewsDetail> {
  return fetchJsonWithOfflineCache(`/content/news/${id}`, {
    signal,
    cacheKey: `news:${id}`,
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
