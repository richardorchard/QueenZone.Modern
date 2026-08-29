import { reportApiFailure } from '../config/sentry';
import { fetchJson, sendJson, sendMultipart } from './client';
import type {
  ApiPagedResponse,
  ForumCategoryListItem,
  ForumIndexStats,
  ForumPoll,
  ForumPost,
  ForumPostCreated,
  ForumRecentThread,
  ForumTopicCreated,
  ForumTopicDetail,
  ForumTopicListItem,
  ForumTopicWatch,
} from './types';
import type { PageQuery } from './content';
import { isLocalFileFailure } from './errors';
import { appendUploadFile, type UploadFilePart } from './uploadFile';

function pageParams({ page, pageSize }: PageQuery) {
  return {
    page,
    pageSize,
  };
}

/** Cross-board recent-activity feed, most-recent first. Used by the home screen. */
export function fetchForumRecentThreads(
  count: number,
  signal?: AbortSignal,
): Promise<ForumRecentThread[]> {
  return fetchJson('/forum/recent-threads', { query: { count }, signal });
}

/** Index totals. `threadCount` is the website `GetForumThreadCountAsync` value. */
export function fetchForumStats(signal?: AbortSignal): Promise<ForumIndexStats> {
  return fetchJson('/forum/stats', { signal });
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

export type ForumTopicWrite = {
  title: string;
  body: string;
  file?: UploadFilePart;
};

export type ForumReplyWrite = {
  body: string;
  file?: UploadFilePart;
};

async function postForumWrite<T>(
  path: string,
  fields: Record<string, string>,
  file: UploadFilePart | undefined,
  accessToken: string,
  signal?: AbortSignal,
): Promise<T> {
  if (!file) {
    return sendJson(path, {
      method: 'POST',
      body: fields,
      accessToken,
      signal,
    });
  }

  const form = new FormData();
  for (const [name, value] of Object.entries(fields)) {
    form.append(name, value);
  }

  try {
    await appendUploadFile(form, 'file', file, signal);
  } catch (err) {
    if (isLocalFileFailure(err)) {
      reportApiFailure({
        kind: err.kind,
        status: err.status,
        method: 'POST',
        path,
        cause: err.cause,
      });
    }
    throw err;
  }

  return sendMultipart(path, form, { accessToken, signal });
}

export function createForumTopic(
  categoryId: number,
  input: ForumTopicWrite,
  accessToken: string,
  signal?: AbortSignal,
): Promise<ForumTopicCreated> {
  return postForumWrite(
    `/forum/categories/${categoryId}/topics`,
    { title: input.title, body: input.body },
    input.file,
    accessToken,
    signal,
  );
}

export function createForumReply(
  topicId: number,
  input: ForumReplyWrite,
  accessToken: string,
  signal?: AbortSignal,
): Promise<ForumPostCreated> {
  return postForumWrite(
    `/forum/topics/${topicId}/posts`,
    { body: input.body },
    input.file,
    accessToken,
    signal,
  );
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

export function fetchForumTopicWatch(
  topicId: number,
  accessToken: string,
  signal?: AbortSignal,
): Promise<ForumTopicWatch> {
  return fetchJson(`/forum/topics/${topicId}/watch`, { accessToken, signal });
}

export function watchForumTopic(
  topicId: number,
  accessToken: string,
  signal?: AbortSignal,
): Promise<ForumTopicWatch> {
  return sendJson(`/forum/topics/${topicId}/watch`, {
    method: 'POST',
    accessToken,
    signal,
  });
}

export function unwatchForumTopic(
  topicId: number,
  accessToken: string,
  signal?: AbortSignal,
): Promise<ForumTopicWatch> {
  return sendJson(`/forum/topics/${topicId}/watch`, {
    method: 'DELETE',
    accessToken,
    signal,
  });
}
