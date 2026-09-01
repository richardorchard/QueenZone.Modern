import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import type { ConversationDetail } from '../../api/messages';
import type { OfflineQueueItem } from '../../offlineQueue';
import {
  isStaleReadFailure,
  messageFromUnknownError,
  overlayQueuedMessages,
  queueStatusLabel,
} from './conversationMeta.ts';

const conversationId = 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee';
const memberId = 'member-1';

function detailWith(messages: ConversationDetail['messages']): ConversationDetail {
  return {
    conversationId,
    otherParticipantId: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
    otherParticipantDisplayName: 'Bob',
    messages,
    page: 1,
    pageSize: 50,
    totalCount: messages.length,
    totalPages: 1,
    detailPath: `/messages/${conversationId}`,
    canSendReply: true,
    hasBlockedOtherParticipant: false,
  };
}

/** Mimics the shape `isApiError` duck-types, without importing the real `ApiError` class. */
function fakeApiError(message: string, kind: 'offline' | 'timeout' | 'http'): Error & { kind: string } {
  const error = new Error(message) as Error & { kind: string };
  error.name = 'ApiError';
  error.kind = kind;
  return error;
}

function queuedItem(overrides: Partial<OfflineQueueItem> = {}): OfflineQueueItem {
  return {
    schemaVersion: 1,
    operationId: 'op-queued',
    memberId,
    kind: 'message.reply',
    target: { conversationId },
    payload: { body: 'Queued hello' },
    createdAt: '2026-08-19T12:05:00.000Z',
    updatedAt: '2026-08-19T12:05:00.000Z',
    attemptCount: 0,
    nextRetryAt: '2026-08-19T12:05:00.000Z',
    state: 'queued',
    lastError: null,
    ...overrides,
  };
}

describe('conversationMeta', () => {
  describe('overlayQueuedMessages', () => {
    it('returns the detail messages unchanged when there is nothing queued', () => {
      const detail = detailWith([]);
      assert.deepEqual(overlayQueuedMessages(detail, [], conversationId, memberId), []);
    });

    it('returns an empty list for a null detail with no queue items', () => {
      assert.deepEqual(overlayQueuedMessages(null, [], conversationId, memberId), []);
    });

    it('ignores queue items without a conversationId or memberId to match against', () => {
      const detail = detailWith([]);
      const item = queuedItem();
      assert.deepEqual(overlayQueuedMessages(detail, [item], null, memberId), []);
      assert.deepEqual(overlayQueuedMessages(detail, [item], conversationId, null), []);
    });

    it('appends a queued reply belonging to this conversation as a display message', () => {
      const detail = detailWith([]);
      const item = queuedItem();
      const [message] = overlayQueuedMessages(detail, [item], conversationId, memberId);
      assert.equal(message.id, item.operationId);
      assert.equal(message.senderMemberId, memberId);
      assert.equal(message.senderDisplayName, 'You');
      assert.equal(message.body, 'Queued hello');
      assert.equal(message.isMine, true);
      assert.equal(message.sortKey, Number.MAX_SAFE_INTEGER);
      assert.equal(message.reportedByViewer, false);
      assert.equal(message.queueState, 'queued');
      assert.equal(message.queueError, null);
    });

    it('ignores a queue item for a different conversation', () => {
      const detail = detailWith([]);
      const item = queuedItem({ target: { conversationId: 'other-conversation' } });
      assert.deepEqual(overlayQueuedMessages(detail, [item], conversationId, memberId), []);
    });

    it('ignores a queue item whose operation already landed in the detail messages', () => {
      const detail = detailWith([
        {
          id: 'op-queued',
          senderMemberId: memberId,
          senderDisplayName: 'You',
          body: 'Already sent',
          createdAt: '2026-08-19T12:06:00.000Z',
          isMine: true,
          sortKey: 1,
          reportedByViewer: false,
        },
      ]);
      const item = queuedItem();
      const result = overlayQueuedMessages(detail, [item], conversationId, memberId);
      assert.equal(result.length, 1);
      assert.equal(result[0].body, 'Already sent');
    });
  });

  describe('queueStatusLabel', () => {
    it('labels a sending item', () => {
      assert.equal(queueStatusLabel('sending'), 'Sending…');
    });

    it('labels an item needing attention', () => {
      assert.equal(queueStatusLabel('needs_attention'), 'Needs attention');
    });

    it('defaults other states to Queued', () => {
      assert.equal(queueStatusLabel('queued'), 'Queued');
    });
  });

  describe('messageFromUnknownError', () => {
    it('uses the ApiError message', () => {
      assert.equal(
        messageFromUnknownError(fakeApiError('The server had a problem.', 'http')),
        'The server had a problem.',
      );
    });

    it('falls back to a generic message for a non-ApiError', () => {
      assert.equal(messageFromUnknownError(new Error('boom')), 'Something went wrong.');
    });
  });

  describe('isStaleReadFailure', () => {
    it('treats an offline ApiError as a stale-read failure', () => {
      assert.equal(isStaleReadFailure(fakeApiError('offline', 'offline')), true);
    });

    it('treats a timeout ApiError as a stale-read failure', () => {
      assert.equal(isStaleReadFailure(fakeApiError('timeout', 'timeout')), true);
    });

    it('treats an http ApiError as not a stale-read failure', () => {
      assert.equal(isStaleReadFailure(fakeApiError('bad request', 'http')), false);
    });

    it('treats a generic error as not a stale-read failure', () => {
      assert.equal(isStaleReadFailure(new Error('boom')), false);
    });
  });
});
