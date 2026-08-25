import {
  closeForumTopicPoll,
  createForumReply,
  createForumTopic,
  fetchForumCategories,
  fetchForumCategory,
  fetchForumCategoryTopics,
  fetchForumRecentThreads,
  fetchForumTopic,
  fetchForumTopicPoll,
  fetchForumTopicPosts,
  voteForumTopicPoll,
} from './forum';
import { jsonResponse } from '../test/fixtures';

jest.mock('../config', () => ({
  apiV1Url: (path: string) => `http://qz.test/api/v1${path.startsWith('/') ? path : `/${path}`}`,
}));

const fetchMock = jest.fn<Promise<Response>, [RequestInfo | URL, RequestInit?]>();

beforeEach(() => {
  fetchMock.mockReset();
  global.fetch = fetchMock as unknown as typeof fetch;
});

function lastCall() {
  const call = fetchMock.mock.calls.at(-1);
  if (!call) {
    throw new Error('fetch was not called');
  }
  return { url: String(call[0]), init: call[1] ?? {} };
}

describe('read endpoints', () => {
  it('builds recent-threads, category, and topic URLs', async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse([]));
    await fetchForumRecentThreads(5);
    expect(lastCall().url).toBe('http://qz.test/api/v1/forum/recent-threads?count=5');

    fetchMock.mockResolvedValueOnce(jsonResponse({ items: [] }));
    await fetchForumCategories({ page: 2 });
    expect(lastCall().url).toBe('http://qz.test/api/v1/forum/categories?page=2');

    fetchMock.mockResolvedValueOnce(jsonResponse({ id: 1 }));
    await fetchForumCategory(1);
    expect(lastCall().url).toBe('http://qz.test/api/v1/forum/categories/1');

    fetchMock.mockResolvedValueOnce(jsonResponse({ items: [] }));
    await fetchForumCategoryTopics(1, { page: 3 });
    expect(lastCall().url).toBe('http://qz.test/api/v1/forum/categories/1/topics?page=3');

    fetchMock.mockResolvedValueOnce(jsonResponse({ id: 10 }));
    await fetchForumTopic(10);
    expect(lastCall().url).toBe('http://qz.test/api/v1/forum/topics/10');

    fetchMock.mockResolvedValueOnce(jsonResponse({ items: [] }));
    await fetchForumTopicPosts(10, { page: 2 });
    expect(lastCall().url).toBe('http://qz.test/api/v1/forum/topics/10/posts?page=2');
  });
});

describe('createForumTopic and createForumReply', () => {
  it('POSTs the title/body with a Bearer token', async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse({ id: 1, starterPostId: 2, title: 'Hi', detailPath: '/t/1' }));
    await createForumTopic(4, { title: 'Hi', body: 'Hello' }, 'tok');
    const { url, init } = lastCall();
    expect(url).toBe('http://qz.test/api/v1/forum/categories/4/topics');
    expect(init.method).toBe('POST');
    expect(init.body).toBe(JSON.stringify({ title: 'Hi', body: 'Hello' }));
    expect(init.headers).toMatchObject({ Authorization: 'Bearer tok' });

    fetchMock.mockResolvedValueOnce(jsonResponse({ id: 9, topicId: 10, detailPath: '/t/10' }));
    await createForumReply(10, { body: 'Reply' }, 'tok');
    const reply = lastCall();
    expect(reply.url).toBe('http://qz.test/api/v1/forum/topics/10/posts');
    expect(reply.init.body).toBe(JSON.stringify({ body: 'Reply' }));
  });
});

describe('poll endpoints', () => {
  it('fetches the poll with an optional viewer token', async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse({ pollId: 'p1' }));
    await fetchForumTopicPoll(10, 'tok');
    const { url, init } = lastCall();
    expect(url).toBe('http://qz.test/api/v1/forum/topics/10/poll');
    expect(init.headers).toMatchObject({ Authorization: 'Bearer tok' });
  });

  it('votes a single option as optionId and multiple as optionIds', async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse({ pollId: 'p1' }));
    await voteForumTopicPoll(10, ['opt-1'], 'tok');
    expect(lastCall().init.body).toBe(JSON.stringify({ optionId: 'opt-1' }));

    fetchMock.mockResolvedValueOnce(jsonResponse({ pollId: 'p1' }));
    await voteForumTopicPoll(10, ['opt-1', 'opt-2'], 'tok');
    expect(lastCall().init.body).toBe(JSON.stringify({ optionIds: ['opt-1', 'opt-2'] }));
  });

  it('closes the poll with a Bearer token and no body', async () => {
    fetchMock.mockResolvedValueOnce(jsonResponse({ pollId: 'p1', isClosed: true }));
    await closeForumTopicPoll(10, 'tok');
    const { url, init } = lastCall();
    expect(url).toBe('http://qz.test/api/v1/forum/topics/10/poll/close');
    expect(init.method).toBe('POST');
    expect(init.headers).toMatchObject({ Authorization: 'Bearer tok' });
  });
});
