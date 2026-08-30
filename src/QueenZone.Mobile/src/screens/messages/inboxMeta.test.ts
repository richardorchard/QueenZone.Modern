import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import {
  buildThreadItems,
  formatDateDividerLabel,
  formatMessageClockTime,
  formatMessageTimestamp,
  inboxPageSize,
  inboxRowA11yLabel,
  initialsFor,
  messagesA11yLabel,
  parseConversationId,
  profileA11yLabel,
  unreadBadgeLabel,
  conversationBodyMaxLength,
  replyRequiredMessage,
  replyTooLongMessage,
  reportReasonMaxLength,
  reportReasonTooLongMessage,
  sendingBlockedNotice,
  unableToSendMessage,
  validateReplyBody,
  validateReportReason,
  youHaveBlockedThisMemberMessage,
} from './inboxMeta.ts';

describe('inboxMeta', () => {
  it('matches website inbox page size', () => {
    assert.equal(inboxPageSize, 50);
  });

  it('accepts conversation GUIDs only', () => {
    assert.equal(parseConversationId('aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee'), 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee');
    assert.equal(parseConversationId('sample'), null);
    assert.equal(parseConversationId(undefined), null);
  });

  it('matches website unread copy', () => {
    assert.equal(unreadBadgeLabel(0), '');
    assert.equal(unreadBadgeLabel(1), '1 unread');
    assert.equal(messagesA11yLabel(0), 'Messages');
    assert.equal(messagesA11yLabel(2), 'Messages, 2 unread conversations');
    assert.equal(profileA11yLabel(1), 'Profile, 1 unread conversations');
  });

  it('builds an inbox row name including unread', () => {
    assert.equal(
      inboxRowA11yLabel({
        otherParticipantDisplayName: 'Roger',
        lastMessagePreview: 'See you at Wembley',
        unreadCount: 1,
      }),
      'Roger. See you at Wembley. 1 unread',
    );
  });

  it('formats timestamps when the date is valid', () => {
    assert.equal(formatMessageTimestamp('not-a-date'), '');
    assert.match(formatMessageTimestamp('2026-08-19T12:00:00Z'), /2026/);
  });

  it('validates reply bodies the same way as the website', () => {
    assert.equal(validateReplyBody('   '), replyRequiredMessage);
    assert.equal(validateReplyBody('a'.repeat(4001)), replyTooLongMessage);
    assert.equal(validateReplyBody('Hello back'), null);
    assert.equal(validateReplyBody('<script>alert(1)</script>'), null);
    assert.equal(conversationBodyMaxLength, 4000);
    assert.equal(unableToSendMessage, 'Unable to send message.');
  });

  it('validates optional report reasons', () => {
    assert.equal(validateReportReason(''), null);
    assert.equal(validateReportReason('Harassment'), null);
    assert.equal(validateReportReason('a'.repeat(1001)), reportReasonTooLongMessage);
    assert.equal(reportReasonMaxLength, 1000);
  });

  it('picks the same sending-blocked notice priority as the website conversation page', () => {
    assert.equal(sendingBlockedNotice(false, true), null);
    assert.equal(sendingBlockedNotice(false, false), unableToSendMessage);
    assert.equal(sendingBlockedNotice(true, false), youHaveBlockedThisMemberMessage);
    // The blocked-by-you notice wins even if canSendReply were somehow true.
    assert.equal(sendingBlockedNotice(true, true), youHaveBlockedThisMemberMessage);
    assert.equal(
      youHaveBlockedThisMemberMessage,
      'You have blocked this member. They can no longer send you private messages.',
    );
  });

  it('builds monogram initials from the first and last name parts', () => {
    assert.equal(initialsFor('Richard Orchard TW'), 'RT');
    assert.equal(initialsFor('Roger'), 'RO');
    assert.equal(initialsFor('  '), '');
  });

  it('formats a 24-hour clock time for message attribution lines', () => {
    assert.equal(formatMessageClockTime('not-a-date'), '');
    assert.match(formatMessageClockTime('2026-08-19T17:05:00Z'), /^\d{2}:\d{2}$/);
  });

  it('labels the date divider using TODAY/YESTERDAY within the last two days', () => {
    const now = new Date(2026, 7, 29, 9, 0, 0);
    assert.equal(formatDateDividerLabel(new Date(2026, 7, 29, 8, 0, 0).toISOString(), now), 'TODAY');
    assert.equal(formatDateDividerLabel(new Date(2026, 7, 28, 8, 0, 0).toISOString(), now), 'YESTERDAY');
    assert.equal(formatDateDividerLabel(new Date(2026, 7, 2, 17, 10, 0).toISOString(), now), '2 AUGUST 2026');
    assert.equal(formatDateDividerLabel('not-a-date', now), '');
  });

  it('groups thread messages into date dividers and runs', () => {
    const items = buildThreadItems([
      { id: 'm1', senderMemberId: 'bob', createdAt: new Date(2026, 7, 2, 17, 10, 0).toISOString() },
      { id: 'm2', senderMemberId: 'bob', createdAt: new Date(2026, 7, 2, 17, 11, 0).toISOString() },
      { id: 'm3', senderMemberId: 'alice', createdAt: new Date(2026, 7, 3, 9, 0, 0).toISOString() },
    ]);

    assert.deepEqual(
      items.map((item) => (item.kind === 'divider' ? 'divider' : `message:${item.isFirstOfRun}`)),
      ['divider', 'message:true', 'message:false', 'divider', 'message:true'],
    );
  });
});
