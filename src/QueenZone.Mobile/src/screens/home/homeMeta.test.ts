import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import {
  formatForumThreadMeta,
  formatGalleryCardMeta,
  liveStripIsVisible,
  liveStripLabel,
  onThisDayIsVisible,
  relativeTimeFromNow,
  stockImageIndexForId,
  visibleSectionsForFilter,
} from './homeMeta.ts';

describe('home meta', () => {
  it('maps each filter chip to its own section set', () => {
    assert.deepEqual(
      [...visibleSectionsForFilter('all')].sort(),
      ['forum', 'gallery', 'hero', 'news', 'onThisDay'].sort(),
    );
    assert.deepEqual([...visibleSectionsForFilter('news')].sort(), ['hero', 'news'].sort());
    assert.deepEqual([...visibleSectionsForFilter('forum')], ['forum']);
    assert.deepEqual([...visibleSectionsForFilter('photography')], ['gallery']);
    assert.deepEqual([...visibleSectionsForFilter('timeline')], ['onThisDay']);
  });

  it('formats forum thread meta with board, reply count, and relative time', () => {
    const parts = formatForumThreadMeta({
      categoryName: 'Live & Tours',
      replyCount: 18,
      lastActivityAt: new Date(Date.now() - 20 * 60000).toISOString(),
    });
    assert.equal(parts[0], 'Live & Tours');
    assert.equal(parts[1], '18 replies');
    assert.equal(parts[2], '20 min ago');
  });

  it('formats gallery card meta without a fabricated "new" count', () => {
    assert.equal(formatGalleryCardMeta({ imageCount: 968 }), '968 images');
  });

  it('hides the on-this-day band only when there is no event', () => {
    assert.equal(onThisDayIsVisible(null), false);
    assert.equal(
      onThisDayIsVisible({
        id: 1,
        title: 'x',
        summary: 'x',
        eventDate: '1980-08-22',
        formattedDate: '22 August 1980',
        category: 'music',
        categoryLabel: 'Release',
        sourceUrl: null,
      }),
      true,
    );
  });

  it('hides the live strip at zero and labels singular vs plural replies', () => {
    assert.equal(liveStripIsVisible(0), false);
    assert.equal(liveStripIsVisible(1), true);
    assert.equal(liveStripLabel(1), '1 new forum reply today');
    assert.equal(liveStripLabel(14), '14 new forum replies today');
  });

  it('cycles a stock image index deterministically by id', () => {
    assert.equal(stockImageIndexForId(0, 5), 0);
    assert.equal(stockImageIndexForId(5, 5), 0);
    assert.equal(stockImageIndexForId(7, 5), 2);
    assert.equal(stockImageIndexForId(7, 5), stockImageIndexForId(7, 5));
  });

  it('formats relative time across the minute/hour/day/date thresholds', () => {
    const now = new Date('2026-08-24T12:00:00Z');
    assert.equal(relativeTimeFromNow(new Date('2026-08-24T11:59:30Z').toISOString(), now), 'just now');
    assert.equal(relativeTimeFromNow(new Date('2026-08-24T11:40:00Z').toISOString(), now), '20 min ago');
    assert.equal(relativeTimeFromNow(new Date('2026-08-24T09:00:00Z').toISOString(), now), '3 hr ago');
    assert.equal(relativeTimeFromNow(new Date('2026-08-22T12:00:00Z').toISOString(), now), '2 days ago');
    assert.equal(relativeTimeFromNow('not-a-date', now), '');
  });
});
