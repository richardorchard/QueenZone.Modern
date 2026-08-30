import { classifyQueueFailure, exhaustedRetries, nextRetryAt } from './retry';
import {
  listOfflineQueue,
  removeOfflineItem,
  updateOfflineItem,
} from './store';
import type { OfflineQueueAuth, OfflineQueueItem } from './types';

type QueueSenders = {
  createForumReply: (
    topicId: number,
    input: { body: string },
    accessToken: string,
    signal?: AbortSignal,
    idempotencyKey?: string,
  ) => Promise<unknown>;
  replyToConversation: (
    accessToken: string,
    conversationId: string,
    body: string,
    signal?: AbortSignal,
    idempotencyKey?: string,
  ) => Promise<unknown>;
  composeMessage: (
    accessToken: string,
    recipientMemberId: string,
    body: string,
    signal?: AbortSignal,
    idempotencyKey?: string,
  ) => Promise<unknown>;
};

let auth: OfflineQueueAuth | null = null;
let flushing = false;
let senders: QueueSenders | null = null;
let retryTimer: ReturnType<typeof setTimeout> | null = null;

export function clearOfflineQueueRetryTimer(): void {
  if (retryTimer) {
    clearTimeout(retryTimer);
    retryTimer = null;
  }
}

async function armRetryTimer(memberId: string): Promise<void> {
  clearOfflineQueueRetryTimer();
  const upcoming = (await listOfflineQueue(memberId))
    .filter((item) => item.state === 'queued')
    .map((item) => Date.parse(item.nextRetryAt))
    .filter((stamp) => Number.isFinite(stamp))
    .sort((a, b) => a - b)[0];
  if (upcoming == null) {
    return;
  }
  const delay = Math.min(Math.max(0, upcoming - Date.now()), 5 * 60_000);
  retryTimer = setTimeout(() => {
    retryTimer = null;
    void flushOfflineQueue();
  }, delay);
}

export function setOfflineQueueSendersForTests(next: QueueSenders | null): void {
  senders = next;
}

async function resolveSenders(): Promise<QueueSenders> {
  if (senders) {
    return senders;
  }
  const forum = await import('../api/forum');
  const messages = await import('../api/messages');
  return {
    createForumReply: forum.createForumReply,
    replyToConversation: messages.replyToConversation,
    composeMessage: messages.composeMessage,
  };
}

export function configureOfflineQueueAuth(next: OfflineQueueAuth | null): void {
  auth = next;
}

function targetKey(item: OfflineQueueItem): string {
  if ('topicId' in item.target) {
    return `forum:${item.target.topicId}`;
  }
  if ('conversationId' in item.target) {
    return `conversation:${item.target.conversationId}`;
  }
  return `compose:${item.target.recipientMemberId}`;
}

async function sendItem(item: OfflineQueueItem, accessToken: string): Promise<void> {
  const body = item.payload.body;
  if (item.kind === 'forum.reply' && 'topicId' in item.target) {
    const active = await resolveSenders();
    await active.createForumReply(
      item.target.topicId,
      { body },
      accessToken,
      undefined,
      item.operationId,
    );
    return;
  }
  if (item.kind === 'message.reply' && 'conversationId' in item.target) {
    const active = await resolveSenders();
    await active.replyToConversation(
      accessToken,
      item.target.conversationId,
      body,
      undefined,
      item.operationId,
    );
    return;
  }
  if (item.kind === 'message.compose' && 'recipientMemberId' in item.target) {
    const active = await resolveSenders();
    await active.composeMessage(
      accessToken,
      item.target.recipientMemberId,
      body,
      undefined,
      item.operationId,
    );
  }
}

export async function flushOfflineQueue(): Promise<void> {
  if (flushing) {
    return;
  }
  const currentAuth = auth;
  if (!currentAuth) {
    return;
  }

  flushing = true;
  let memberId: string | null = null;
  try {
    memberId = currentAuth.getMemberId();
    if (!memberId) {
      return;
    }

    const accessToken = (await currentAuth.refreshAccessToken()) ?? currentAuth.getAccessToken();
    if (!accessToken) {
      return;
    }

    const now = new Date().toISOString();
    const items = (await listOfflineQueue(memberId))
      .filter((item) => item.state !== 'needs_attention' && item.nextRetryAt <= now)
      .sort((a, b) => a.createdAt.localeCompare(b.createdAt));

    const blockedTargets = new Set<string>();

    for (const item of items) {
      const key = targetKey(item);
      if (blockedTargets.has(key)) {
        continue;
      }

      await updateOfflineItem(item.operationId, { state: 'sending' });
      try {
        await sendItem(item, accessToken);
        await removeOfflineItem(item.operationId);
      } catch (err) {
        const kind = classifyQueueFailure(err);
        const attemptCount = item.attemptCount + 1;
        if (kind === 'permanent' || exhaustedRetries(attemptCount)) {
          await updateOfflineItem(item.operationId, {
            state: 'needs_attention',
            attemptCount,
            lastError: err instanceof Error ? err.message : 'Send failed.',
          });
          continue;
        }

        await updateOfflineItem(item.operationId, {
          state: 'queued',
          attemptCount,
          nextRetryAt: nextRetryAt(attemptCount, err),
          lastError: err instanceof Error ? err.message : 'Send failed.',
        });
        blockedTargets.add(key);
        if (kind === 'auth' || kind === 'systemic' || kind === 'retry') {
          return;
        }
      }
    }
  } finally {
    flushing = false;
    if (memberId) {
      void armRetryTimer(memberId);
    }
  }
}
