import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { newsListItemSchema, parseContract, searchResultSchema } from './schemas.ts';

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
});
