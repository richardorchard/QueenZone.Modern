import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { attachmentMeta, formatMemberSince, formatPostTimestamp, imagePreviewUrl } from './forumThreadMeta.ts';

describe('forum thread meta', () => {
  it('formats a post timestamp with the calendar year', () => {
    const stamp = formatPostTimestamp('2024-06-01T10:00:00Z');
    assert.match(stamp, /2024/);
    assert.notEqual(stamp, '');
  });

  it('returns empty values for invalid dates', () => {
    assert.equal(formatPostTimestamp('not-a-date'), '');
    assert.equal(formatMemberSince('nope'), null);
    assert.equal(formatMemberSince(null), null);
  });

  it('formats member-since with the calendar year', () => {
    const since = formatMemberSince('2004-03-12T00:00:00Z');
    assert.match(since ?? '', /2004/);
  });

  it('joins extension, size, and members-only on attachment captions', () => {
    assert.equal(
      attachmentMeta({ extension: 'JPG', formattedSize: '278.0 KB' }),
      'JPG · 278.0 KB · Members only',
    );
    assert.equal(attachmentMeta({ extension: 'PDF', formattedSize: '' }), 'PDF · Members only');
  });

  it('uses thumbnails for image previews and skips non-images', () => {
    assert.equal(
      imagePreviewUrl({
        isImage: true,
        thumbnailUrl: '/ugc/forum/a-thumb.webp',
        url: '/forum/attachment/1/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
      }),
      '/ugc/forum/a-thumb.webp',
    );
    assert.equal(
      imagePreviewUrl({
        isImage: true,
        thumbnailUrl: null,
        url: '/forum/attachment/legacy/1002',
      }),
      '/forum/attachment/legacy/1002',
    );
    assert.equal(
      imagePreviewUrl({
        isImage: false,
        thumbnailUrl: null,
        url: '/forum/attachment/legacy/9',
      }),
      null,
    );
  });
});
