import type { NotificationDestination } from './payload';

/**
 * Private-message unread generation. A privateMessage receive increments it
 * so a mounted/focused tab-root masthead can refetch
 * GET /api/v1/me/messages/unread-count. This is not the news list epoch.
 */
let generation = 0;
const listeners = new Set<() => void>();

export function getPmUnreadEpoch(): number {
  return generation;
}

export function subscribePmUnreadEpoch(listener: () => void): () => void {
  listeners.add(listener);
  return () => {
    listeners.delete(listener);
  };
}

export function bumpPmUnreadEpoch(): void {
  generation += 1;
  for (const listener of listeners) {
    listener();
  }
}

export function notePmUnreadPush(destination: NotificationDestination): void {
  if (destination.category === 'privateMessage') {
    bumpPmUnreadEpoch();
  }
}
