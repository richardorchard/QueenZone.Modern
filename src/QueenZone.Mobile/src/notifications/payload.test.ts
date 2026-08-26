import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { fallbackNoticeCopy, noticeEyebrow, parseNotificationData } from './payload.ts';

const conversationId = '11111111-2222-3333-4444-555555555555';

describe('parseNotificationData', () => {
  it('maps a forum-reply payload to the thread, including optional postId', () => {
    assert.deepEqual(
      parseNotificationData({ category: 'forumReply', topicId: '12', postId: '34' }),
      { category: 'forumReply', topicId: 12, postId: 34 },
    );
    assert.deepEqual(parseNotificationData({ category: 'forumReply', topicId: 1002 }), {
      category: 'forumReply',
      topicId: 1002,
    });
  });

  it('maps a private-message payload to the conversation', () => {
    assert.deepEqual(parseNotificationData({ category: 'privateMessage', conversationId }), {
      category: 'privateMessage',
      conversationId,
    });
  });

  it('maps a news payload to the article', () => {
    assert.deepEqual(parseNotificationData({ category: 'news', articleId: '88' }), {
      category: 'news',
      articleId: 88,
    });
  });

  it('accepts a JSON string data blob (some transports stringify the dictionary)', () => {
    assert.deepEqual(parseNotificationData(JSON.stringify({ category: 'news', articleId: '1003' })), {
      category: 'news',
      articleId: 1003,
    });
  });

  it('ignores extra keys and an invalid optional postId', () => {
    assert.deepEqual(
      parseNotificationData({
        category: 'forumReply',
        topicId: '12',
        postId: 'nope',
        title: 'not a contract key',
      }),
      { category: 'forumReply', topicId: 12 },
    );
  });

  it('rejects missing or unknown contract fields', () => {
    assert.equal(parseNotificationData(null), null);
    assert.equal(parseNotificationData({}), null);
    assert.equal(parseNotificationData({ category: 'digest' }), null);
    assert.equal(parseNotificationData({ category: 'forumReply' }), null);
    assert.equal(parseNotificationData({ category: 'forumReply', topicId: 0 }), null);
    assert.equal(parseNotificationData({ category: 'privateMessage', conversationId: 'not-a-guid' }), null);
    assert.equal(parseNotificationData({ category: 'news', articleId: '-1' }), null);
    assert.equal(parseNotificationData('not-json'), null);
  });
});

describe('fallbackNoticeCopy', () => {
  it('matches the #757 human title/body for each category', () => {
    assert.deepEqual(fallbackNoticeCopy({ category: 'forumReply', topicId: 1 }), {
      title: 'New forum reply',
      body: 'New reply',
    });
    assert.deepEqual(fallbackNoticeCopy({ category: 'privateMessage', conversationId }), {
      title: 'New private message',
      body: 'You have a new message.',
    });
    assert.deepEqual(fallbackNoticeCopy({ category: 'news', articleId: 1 }), {
      title: 'New QueenZone article',
      body: 'New article published.',
    });
  });
});

describe('noticeEyebrow', () => {
  it('labels the in-app banner by category', () => {
    assert.equal(noticeEyebrow('forumReply'), 'Forum');
    assert.equal(noticeEyebrow('privateMessage'), 'Message');
    assert.equal(noticeEyebrow('news'), 'News');
  });
});
