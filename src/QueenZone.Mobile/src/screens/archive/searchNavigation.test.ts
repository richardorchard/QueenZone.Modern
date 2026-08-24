import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import type { SearchResult } from '../../api/types';
import { applySearchTarget, targetForSearchResult } from './searchNavigation.ts';

function hit(overrides: Partial<SearchResult>): SearchResult {
  return {
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
    ...overrides,
  };
}

describe('targetForSearchResult', () => {
  const origin = 'https://www.queenzone.org';

  it('opens news, forum, biography, discography, and fan performances on real ids', () => {
    assert.deepEqual(targetForSearchResult(hit({}), origin), {
      kind: 'tab',
      tab: 'NewsTab',
      screen: 'Story',
      params: { id: 1003 },
    });
    assert.deepEqual(
      targetForSearchResult(
        hit({
          contentType: 'forum',
          sourceKey: 'forum-thread:1002',
          url: '/forum/topic/1002/ranking',
          id: 1002,
        }),
        origin,
      ),
      {
        kind: 'tab',
        tab: 'ForumTab',
        screen: 'Thread',
        params: { id: 1002 },
      },
    );
    assert.deepEqual(
      targetForSearchResult(hit({ contentType: 'biography', sourceKey: 'biography:7', id: 7 }), origin),
      {
        kind: 'tab',
        tab: 'ArchiveTab',
        screen: 'BiographyChapter',
        params: { id: 7 },
      },
    );
    assert.deepEqual(
      targetForSearchResult(hit({ contentType: 'discography', sourceKey: 'discography:3', id: 3 }), origin),
      {
        kind: 'tab',
        tab: 'ArchiveTab',
        screen: 'Album',
        params: { id: 3 },
      },
    );
    assert.deepEqual(
      targetForSearchResult(
        hit({ contentType: 'fan-performance', sourceKey: 'fan-performance:187', id: 187 }),
        origin,
      ),
      {
        kind: 'tab',
        tab: 'ArchiveTab',
        screen: 'FanPerformanceDetail',
        params: { id: 187 },
      },
    );
  });

  it('opens the timeline list rather than a missing event screen', () => {
    assert.deepEqual(
      targetForSearchResult(hit({ contentType: 'timeline', sourceKey: 'timeline:12', id: 12 }), origin),
      { kind: 'tab', tab: 'ArchiveTab', screen: 'Timeline' },
    );
  });

  it('opens articles in the in-app browser and rejects placeholder ids', () => {
    assert.deepEqual(
      targetForSearchResult(
        hit({
          contentType: 'article',
          sourceKey: 'article:some-slug',
          url: '/articles/some-slug',
          id: null,
        }),
        origin,
      ),
      { kind: 'web', url: 'https://www.queenzone.org/articles/some-slug' },
    );

    assert.deepEqual(targetForSearchResult(hit({ id: 0, sourceKey: 'news:0' }), origin), {
      kind: 'unsupported',
    });
    assert.deepEqual(
      targetForSearchResult(
        hit({
          contentType: 'forum',
          sourceKey: 'forum-thread:magic-tour',
          id: null,
        }),
        origin,
      ),
      { kind: 'unsupported' },
    );
  });

  it('applies tab and web targets without placeholder ids', () => {
    const navigated: unknown[] = [];
    const opened: string[] = [];
    applySearchTarget(
      { kind: 'tab', tab: 'NewsTab', screen: 'Story', params: { id: 1003 } },
      (tab, params) => {
        navigated.push([tab, params]);
      },
      (url) => {
        opened.push(url);
      },
    );
    applySearchTarget({ kind: 'tab', tab: 'ArchiveTab', screen: 'Timeline' }, (tab, params) => {
      navigated.push([tab, params]);
    }, () => {});
    applySearchTarget({ kind: 'web', url: 'https://www.queenzone.org/articles/x' }, () => {}, (url) => {
      opened.push(url);
    });
    applySearchTarget({ kind: 'unsupported' }, (tab, params) => {
      navigated.push([tab, params]);
    }, () => {});

    assert.deepEqual(navigated[0], ['NewsTab', { screen: 'Story', params: { id: 1003 } }]);
    assert.deepEqual(navigated[1], ['ArchiveTab', { screen: 'Timeline' }]);
    assert.deepEqual(opened, ['https://www.queenzone.org/articles/x']);
    assert.equal(navigated.length, 2);
  });
});
