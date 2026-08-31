import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import {
  attachmentAction,
  attachmentMeta,
  formatMemberSince,
  formatPostTimestamp,
  imagePreviewUrl,
  parseTopicId,
  topicReplyAllowed,
  watchButtonLabel,
  watchHint,
} from './forumThreadMeta.ts';

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

  it('hides reply when the topic is locked', () => {
    assert.equal(topicReplyAllowed({ isLocked: false }), true);
    assert.equal(topicReplyAllowed({ isLocked: true }), false);
    assert.equal(topicReplyAllowed(null), true);
  });

  it('labels Watch versus Unwatch without implying auto-subscribe', () => {
    assert.equal(watchButtonLabel(false), 'Watch topic');
    assert.equal(watchButtonLabel(true), 'Unwatch');
    assert.match(watchHint(false), /does not subscribe/i);
    assert.match(watchHint(true), /someone else replies/i);
  });

  it('parses numeric topic ids and rejects prototype slugs', () => {
    assert.equal(parseTopicId(1002), 1002);
    assert.equal(parseTopicId('1002'), 1002);
    assert.equal(parseTopicId('magic-tour'), null);
    assert.equal(parseTopicId(0), null);
  });

  it('inlines images only when a thumbnail URL is present', () => {
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
      null,
    );
    assert.equal(
      imagePreviewUrl({
        isImage: true,
        thumbnailUrl: '   ',
        url: '/forum/attachment/legacy/1002',
      }),
      null,
    );
    assert.equal(
      imagePreviewUrl({
        isImage: false,
        thumbnailUrl: '/ugc/forum/a-thumb.webp',
        url: '/forum/attachment/legacy/9',
      }),
      null,
    );
    assert.equal(
      imagePreviewUrl({
        isImage: true,
        thumbnailUrl: '/forum/attachment/legacy/1002',
        url: '/forum/attachment/legacy/1002',
      }),
      null,
    );
    assert.equal(
      imagePreviewUrl({
        isImage: true,
        thumbnailUrl: '/forum/attachment/1/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
        url: '/api/v1/forum/attachments/1/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
      }),
      null,
    );
  });

  it('opens signed-in images with or without a thumb and stays inert when signed out', () => {
    const legacyJpg = {
      isImage: true,
      thumbnailUrl: null,
      url: '/forum/attachment/legacy/1002',
    };
    const thumbed = {
      isImage: true,
      thumbnailUrl: '/ugc/forum/a-thumb.webp',
      url: '/forum/attachment/1/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
    };
    const pdf = {
      isImage: false,
      thumbnailUrl: null,
      url: '/forum/attachment/legacy/1101',
    };
    assert.equal(attachmentAction(legacyJpg, true), 'view-image');
    assert.equal(attachmentAction(legacyJpg, false), 'none');
    assert.equal(attachmentAction(thumbed, true), 'view-image');
    assert.equal(attachmentAction(pdf, true), 'open-file');
    assert.equal(attachmentAction(pdf, false), 'none');
  });
});
