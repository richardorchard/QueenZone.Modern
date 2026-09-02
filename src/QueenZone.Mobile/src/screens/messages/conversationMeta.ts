import type { ApiError } from '../../api/errors';
import type { ConversationDetail, ConversationMessage } from '../../api/messages';
import type { OfflineQueueItem } from '../../offlineQueue';

export type DisplayMessage = ConversationMessage & {
  queueState?: OfflineQueueItem['state'];
  queueError?: string | null;
};

export function overlayQueuedMessages(
  detail: ConversationDetail | null,
  queueItems: OfflineQueueItem[],
  conversationId: string | null,
  memberId: string | null,
): DisplayMessage[] {
  const messages: DisplayMessage[] = [...(detail?.messages ?? [])];
  if (!conversationId || !memberId) {
    return messages;
  }
  const pending = queueItems.filter(
    (item) =>
      item.kind === 'message.reply' &&
      'conversationId' in item.target &&
      item.target.conversationId === conversationId,
  );
  for (const item of pending) {
    if (messages.some((message) => message.id === item.operationId)) {
      continue;
    }
    messages.push({
      id: item.operationId,
      senderMemberId: memberId,
      senderDisplayName: 'You',
      body: item.payload.body,
      createdAt: item.createdAt,
      isMine: true,
      sortKey: Number.MAX_SAFE_INTEGER,
      reportedByViewer: false,
      queueState: item.state,
      queueError: item.lastError,
    });
  }
  return messages;
}

export function queueStatusLabel(state: OfflineQueueItem['state']): string {
  if (state === 'sending') {
    return 'Sending…';
  }
  if (state === 'needs_attention') {
    return 'Needs attention';
  }
  return 'Queued';
}

/** Duck-types `ApiError` (rather than importing the class) so this file stays free of the
 * `api/client` import chain, which pulls in React Native modules the node:test runner can't resolve. */
function isApiError(err: unknown): err is ApiError {
  return err instanceof Error && err.name === 'ApiError';
}

export function messageFromUnknownError(err: unknown): string {
  return isApiError(err) ? err.message : 'Something went wrong.';
}

export function isStaleReadFailure(err: unknown): boolean {
  return isApiError(err) && (err.kind === 'offline' || err.kind === 'timeout');
}
