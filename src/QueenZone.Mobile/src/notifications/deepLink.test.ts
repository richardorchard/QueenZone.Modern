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

  it('opens the category listing when the detail id is missing', () => {
    assert.deepEqual(notificationNavigateParams({ category: 'forumReply' }), {
      screen: 'ForumTab',
      params: { screen: 'ForumIndex', initial: false },
    });
    assert.deepEqual(notificationNavigateParams({ category: 'privateMessage' }), {
      screen: 'HomeTab',
      params: { screen: 'Inbox', initial: false },
    });
    const newsList = notificationNavigateParams({ category: 'news' });
    assert.equal(newsList.screen, 'NewsTab');
    assert.equal(newsList.params.screen, 'NewsIndex');
    assert.equal(newsList.params.initial, false);
    assert.equal(typeof newsList.params.params?.refreshAt, 'number');
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

  it('opens Thread and Conversation for forum and private-message destinations', () => {
    const calls: unknown[] = [];
    const navigation = {
      navigate: (name: 'Tabs', params: object) => {
        calls.push({ name, params });
      },
    };
    openNotificationDestination(navigation, { category: 'forumReply', topicId: 1002 });
    openNotificationDestination(navigation, { category: 'privateMessage', conversationId });
    assert.deepEqual(calls, [
      {
        name: 'Tabs',
        params: {
          screen: 'ForumTab',
          params: { screen: 'Thread', params: { id: 1002 }, initial: false },
        },
      },
      {
        name: 'Tabs',
        params: {
          screen: 'HomeTab',
          params: { screen: 'Conversation', params: { id: conversationId }, initial: false },
        },
      },
    ]);
  });
});
