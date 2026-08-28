import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { newsDetailSchema, newsListItemSchema, notificationPreferencesSchema, parseContract, searchResultSchema } from './schemas.ts';

describe('parseContract', () => {
  it('names the endpoint and missing field when a payload is incompatible', () => {
    assert.throws(
      () => parseContract('GET /api/v1/content/news', newsListItemSchema, { id: 1, excerpt: '', publishedAt: '2026-01-01', detailPath: '/news/1' }),
      /Contract GET \/api\/v1\/content\/news failed: title:/,
    );
  });

  it('accepts a complete news list item', () => {
    const item = parseContract('GET /api/v1/content/news', newsListItemSchema, {
      id: 1003,
      title: 'QueenZone modernisation begins',
      excerpt: 'Excerpt',
      publishedAt: '2026-06-11T09:00:00',
      detailPath: '/news/1003/queenzone-modernisation-begins',
    });
    assert.equal(item.id, 1003);
    assert.equal(item.imageUrl, undefined);
  });

  it('accepts optional news image urls', () => {
    const withImage = parseContract('GET /api/v1/content/news', newsListItemSchema, {
      id: 1003,
      title: 'QueenZone modernisation begins',
      excerpt: 'Excerpt',
      publishedAt: '2026-06-11T09:00:00',
      detailPath: '/news/1003/queenzone-modernisation-begins',
      imageUrl: '/ugc/articles/editors/me/hero.webp',
      thumbnailUrl: '/ugc/articles/editors/me/hero.webp?size=thumb',
    });
    assert.equal(withImage.imageUrl, '/ugc/articles/editors/me/hero.webp');
    assert.equal(withImage.thumbnailUrl, '/ugc/articles/editors/me/hero.webp?size=thumb');
    const withoutImage = parseContract('GET /api/v1/content/news', newsListItemSchema, {
      id: 1004,
      title: 'No photo',
      excerpt: 'Excerpt',
      publishedAt: '2026-06-11T09:00:00',
      detailPath: '/news/1004/no-photo',
      imageUrl: null,
      thumbnailUrl: null,
    });
    assert.equal(withoutImage.imageUrl, null);
    assert.equal(withoutImage.thumbnailUrl, null);
  });

  it('accepts optional news discussion fields on detail', () => {
    const withoutTopic = parseContract('GET /api/v1/content/news/1003', newsDetailSchema, {
      id: 1003,
      title: 'QueenZone modernisation begins',
      excerpt: 'Excerpt',
      body: '<p>Body</p>',
      publishedAt: '2026-06-11T09:00:00',
      sourceUrl: null,
      detailPath: '/news/1003/queenzone-modernisation-begins',
      topicId: null,
      discussionReplyCount: null,
      discussionPreview: null,
    });
    assert.equal(withoutTopic.topicId, null);

    const withPreview = parseContract('GET /api/v1/content/news/42', newsDetailSchema, {
      id: 42,
      title: 'Linked story',
      excerpt: 'Excerpt',
      body: '<p>Body</p>',
      publishedAt: '2026-08-01T08:00:00Z',
      sourceUrl: null,
      detailPath: '/news/42/linked-story',
      topicId: 1002,
      discussionReplyCount: 2,
      discussionPreview: [
        { authorDisplayName: 'Alice', postedAt: '2026-08-01T10:30:00Z', excerpt: 'First preview excerpt' },
        { authorDisplayName: 'Bob', postedAt: '2026-08-01T11:30:00Z', excerpt: 'Latest preview excerpt' },
      ],
    });
    assert.equal(withPreview.topicId, 1002);
    assert.equal(withPreview.discussionReplyCount, 2);
    assert.equal(withPreview.discussionPreview?.[1]?.authorDisplayName, 'Bob');
  });

  it('accepts a search hit with sourceKey and optional id', () => {
    const item = parseContract('GET /api/v1/search', searchResultSchema, {
      contentType: 'news',
      sourceKey: 'news:1003',
      title: 'QueenZone modernisation begins',
      summary: 'Excerpt',
      url: '/news/1003/queenzone-modernisation-begins',
      publishedAt: '2026-06-11T09:00:00Z',
      imageUrl: null,
      category: null,
      authorDisplayName: null,
      id: 1003,
    });
    assert.equal(item.sourceKey, 'news:1003');
    assert.equal(item.id, 1003);
  });

  it('accepts notification preference toggles', () => {
    const prefs = parseContract('GET /api/v1/me/notification-preferences', notificationPreferencesSchema, {
      forumReply: true,
      privateMessage: true,
      news: false,
    });
    assert.equal(prefs.forumReply, true);
    assert.equal(prefs.news, false);
  });
});
