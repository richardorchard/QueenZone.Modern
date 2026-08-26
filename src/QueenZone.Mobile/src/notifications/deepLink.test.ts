import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { notificationNavigateParams, openNotificationDestination } from './deepLink.ts';

const conversationId = '11111111-2222-3333-4444-555555555555';

describe('notificationNavigateParams', () => {
  it('opens ForumTab / Thread with topicId (and optional postId)', () => {
    assert.deepEqual(notificationNavigateParams({ category: 'forumReply', topicId: 12, postId: 34 }), {
      screen: 'ForumTab',
      params: { screen: 'Thread', params: { id: 12, postId: 34 }, initial: false },
    });
    assert.deepEqual(notificationNavigateParams({ category: 'forumReply', topicId: 12 }), {
      screen: 'ForumTab',
      params: { screen: 'Thread', params: { id: 12 }, initial: false },
    });
  });

  it('opens HomeTab / Conversation with conversationId', () => {
    assert.deepEqual(notificationNavigateParams({ category: 'privateMessage', conversationId }), {
      screen: 'HomeTab',
      params: { screen: 'Conversation', params: { id: conversationId }, initial: false },
    });
  });

  it('opens NewsTab / Story with articleId', () => {
    assert.deepEqual(notificationNavigateParams({ category: 'news', articleId: 88 }), {
      screen: 'NewsTab',
      params: { screen: 'Story', params: { id: 88 }, initial: false },
    });
  });
});

describe('openNotificationDestination', () => {
  it('navigates through the root Tabs screen so cold-start stacks stay consistent', () => {
    const calls: unknown[] = [];
    openNotificationDestination(
      {
        navigate: (name, params) => {
          calls.push({ name, params });
        },
      },
      { category: 'news', articleId: 1003 },
    );
    assert.deepEqual(calls, [
      {
        name: 'Tabs',
        params: {
          screen: 'NewsTab',
          params: { screen: 'Story', params: { id: 1003 }, initial: false },
        },
      },
    ]);
  });
});
