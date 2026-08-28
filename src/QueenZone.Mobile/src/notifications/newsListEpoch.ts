import type { NotificationDestination } from './payload';

/**
 * News-only list generation. A news push receive or tap increments it so
 * mounted Home news and NewsIndex can call refresh(). Other categories
 * and unmounted screens are ignored. This is not a shared news-list store.
 */
let generation = 0;
const listeners = new Set<() => void>();

export function getNewsListEpoch(): number {
  return generation;
}

export function subscribeNewsListEpoch(listener: () => void): () => void {
  listeners.add(listener);
  return () => {
    listeners.delete(listener);
  };
}

export function bumpNewsListEpoch(): void {
  generation += 1;
  for (const listener of listeners) {
    listener();
  }
}

export function noteNewsListPush(destination: NotificationDestination): void {
  if (destination.category === 'news') {
    bumpNewsListEpoch();
  }
}
