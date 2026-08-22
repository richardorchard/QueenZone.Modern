import { fetchJson } from './client';
import type {
  ApiPagedResponse,
  ForumCategoryListItem,
  ForumPost,
  ForumTopicDetail,
  ForumTopicListItem,
} from './types';
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

export function fetchForumTopic(id: number, signal?: AbortSignal): Promise<ForumTopicDetail> {
  return fetchJson(`/forum/topics/${id}`, { signal });
}

export function fetchForumTopicPosts(
  topicId: number,
  query: PageQuery = {},
): Promise<ApiPagedResponse<ForumPost>> {
  return fetchJson(`/forum/topics/${topicId}/posts`, {
    query: pageParams(query),
    signal: query.signal,
  });
}
