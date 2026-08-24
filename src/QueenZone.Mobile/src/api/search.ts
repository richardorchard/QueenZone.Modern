import { fetchJson } from './client';
import type { ApiPagedResponse, SearchResult } from './types';

export type SearchPageQuery = {
  q: string;
  type?: string | null;
  page?: number;
  pageSize?: number;
  signal?: AbortSignal;
};

/** Public whole-site search. Empty `q` is a 200 empty page on the server. */
export function fetchSearchPage(query: SearchPageQuery): Promise<ApiPagedResponse<SearchResult>> {
  return fetchJson('/search', {
    query: {
      q: query.q,
      type: query.type ?? undefined,
      page: query.page,
      pageSize: query.pageSize,
    },
    signal: query.signal,
  });
}
