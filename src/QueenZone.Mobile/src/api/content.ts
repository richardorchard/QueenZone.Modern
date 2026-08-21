import { fetchJsonWithOfflineCache } from '../cache';
import { fetchJson } from './client';
import type {
  AlbumDetail,
  AlbumListItem,
  ApiPagedResponse,
  BiographyChapterDetail,
  BiographyChapterListItem,
  FreddieTribute,
  NewsDetail,
  NewsListItem,
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

export function fetchNewsPage(query: PageQuery = {}): Promise<ApiPagedResponse<NewsListItem>> {
  return fetchJson('/content/news', { query: pageParams(query), signal: query.signal });
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

export function fetchFreddieTributePage(
  query: PageQuery = {},
): Promise<ApiPagedResponse<FreddieTribute>> {
  return fetchJson('/content/freddietribute', { query: pageParams(query), signal: query.signal });
}
