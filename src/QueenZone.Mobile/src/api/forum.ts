import { fetchJson, sendJson } from './client';
import type {
  ApiPagedResponse,
  ForumCategoryListItem,
  ForumPoll,
  ForumPost,
  ForumPostCreated,
  ForumTopicCreated,
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

export function createForumTopic(
  categoryId: number,
  input: { title: string; body: string },
  accessToken: string,
  signal?: AbortSignal,
): Promise<ForumTopicCreated> {
  return sendJson(`/forum/categories/${categoryId}/topics`, {
    method: 'POST',
    body: { title: input.title, body: input.body },
    accessToken,
    signal,
  });
}

export function createForumReply(
  topicId: number,
  input: { body: string },
  accessToken: string,
  signal?: AbortSignal,
): Promise<ForumPostCreated> {
  return sendJson(`/forum/topics/${topicId}/posts`, {
    method: 'POST',
    body: { body: input.body },
    accessToken,
    signal,
  });
}

export function fetchForumTopicPoll(
  topicId: number,
  accessToken?: string | null,
  signal?: AbortSignal,
): Promise<ForumPoll> {
  return fetchJson(`/forum/topics/${topicId}/poll`, { accessToken, signal });
}

export function voteForumTopicPoll(
  topicId: number,
  optionIds: string[],
  accessToken: string,
  signal?: AbortSignal,
): Promise<ForumPoll> {
  return sendJson(`/forum/topics/${topicId}/poll/vote`, {
    method: 'POST',
    body: optionIds.length === 1 ? { optionId: optionIds[0] } : { optionIds },
    accessToken,
    signal,
  });
}

export function closeForumTopicPoll(
  topicId: number,
  accessToken: string,
  signal?: AbortSignal,
): Promise<ForumPoll> {
  return sendJson(`/forum/topics/${topicId}/poll/close`, {
    method: 'POST',
    accessToken,
    signal,
  });
}
