import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import type { ForumCategoryListItem, ForumTopicListItem } from '../../api';
import { categoryMeta, formatForumCount, topicMeta } from './forumListMeta.ts';

const category: ForumCategoryListItem = {
  id: 1,
  name: 'The Music',
  description: 'Albums and songs',
  postCount: 41200,
  lastActivityAt: '2024-06-12T14:00:00Z',
  latestThreadTitle: 'Ranking every studio album',
  detailPath: '/forum/1/the-music',
};

const topic: ForumTopicListItem = {
  id: 1001,
  title: 'Forum Guidelines',
  lastActivityAt: '2024-06-12T20:04:00Z',
  authorUsername: 'Richard Orchard',
  replyCount: 44,
  lastPostUsername: 'waunakonor',
  isSticky: true,
  detailPath: '/forum/topic/1001/forum-guidelines',
};

describe('forum list meta', () => {
  it('prefers the latest thread title on a board card', () => {
    assert.equal(categoryMeta(category), 'Latest: Ranking every studio album');
  });

  it('falls back to last activity, then post count', () => {
    assert.match(
      categoryMeta({ ...category, latestThreadTitle: null }),
      /^Last activity /,
    );
    assert.equal(
      categoryMeta({ ...category, latestThreadTitle: null, lastActivityAt: null }),
      `${formatForumCount(41200)} posts`,
    );
  });

  it('marks pinned topics and includes replies, last poster, and date', () => {
    const meta = topicMeta(topic);
    assert.match(meta, /^Pinned · /);
    assert.match(meta, /44 replies/);
    assert.match(meta, /waunakonor/);
  });

  it('omits pin and last poster when those fields are empty', () => {
    const meta = topicMeta({ ...topic, isSticky: false, lastPostUsername: null, replyCount: 0 });
    assert.equal(meta.startsWith('Pinned'), false);
    assert.match(meta, /^0 replies · /);
  });
});
