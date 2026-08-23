import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import {
  formatMessageTimestamp,
  inboxPageSize,
  inboxRowA11yLabel,
  messagesA11yLabel,
  parseConversationId,
  profileA11yLabel,
  unreadBadgeLabel,
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
});
