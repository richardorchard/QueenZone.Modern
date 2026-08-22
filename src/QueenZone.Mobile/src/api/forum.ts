import { fetchJson } from './client';
import type { ApiPagedResponse, ForumCategoryListItem, ForumTopicListItem } from './types';
import type { PageQuery } from './content';

function pageParams({ page, pageSize }: PageQuery) {
  return {
    page,
    pageSize,
  };
}

export function fetchForumCategories(
  query: PageQuery = {},
): Promise<ApiPagedResponse<ForumCategoryListItem>> {
  return fetchJson('/forum/categories', { query: pageParams(query), signal: query.signal });
}

export function fetchForumCategory(
  id: number,
  signal?: AbortSignal,
): Promise<ForumCategoryListItem> {
  return fetchJson(`/forum/categories/${id}`, { signal });
}

export function fetchForumCategoryTopics(
  categoryId: number,
  query: PageQuery = {},
): Promise<ApiPagedResponse<ForumTopicListItem>> {
  return fetchJson(`/forum/categories/${categoryId}/topics`, {
    query: pageParams(query),
    signal: query.signal,
  });
}
