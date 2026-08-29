import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { categoryMeta, formatForumCount, forumIndexStatItems, topicMeta } from './forumListMeta.ts';

describe('forum list meta', () => {
  it('prefers the latest thread title on a board card', () => {
    assert.equal(
      categoryMeta({
        latestThreadTitle: 'Ranking every studio album',
        lastActivityAt: '2024-06-12T14:00:00Z',
        postCount: 41200,
      }),
      'Latest: Ranking every studio album',
    );
  });

  it('falls back to last activity, then post count', () => {
    assert.match(
      categoryMeta({
        latestThreadTitle: null,
        lastActivityAt: '2024-06-12T14:00:00Z',
        postCount: 41200,
      }),
      /^Last activity /,
    );
    assert.equal(
      categoryMeta({
        latestThreadTitle: null,
        lastActivityAt: null,
        postCount: 41200,
      }),
      `${formatForumCount(41200)} posts`,
    );
  });

  it('marks pinned topics and includes replies, last poster, and date', () => {
    const meta = topicMeta({
      lastActivityAt: '2024-06-12T20:04:00Z',
      replyCount: 44,
      lastPostUsername: 'waunakonor',
      isSticky: true,
    });
    assert.match(meta, /^Pinned · /);
    assert.match(meta, /44 replies/);
    assert.match(meta, /waunakonor/);
  });

  it('omits pin and last poster when those fields are empty', () => {
    const meta = topicMeta({
      lastActivityAt: '2024-06-12T20:04:00Z',
      replyCount: 0,
      lastPostUsername: null,
      isSticky: false,
    });
    assert.equal(meta.startsWith('Pinned'), false);
    assert.match(meta, /^0 replies · /);
  });

  it('formats Boards, Threads, and Posts with locale grouping, website order', () => {
    assert.deepEqual(forumIndexStatItems({ boardCount: 7, threadCount: 12600, postCount: 15 }), [
      { value: formatForumCount(7), label: 'Boards' },
      { value: formatForumCount(12600), label: 'Threads' },
      { value: formatForumCount(15), label: 'Posts' },
    ]);
  });
});
