import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { newsListItemSchema, notificationPreferencesSchema, parseContract, searchResultSchema } from './schemas.ts';

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
