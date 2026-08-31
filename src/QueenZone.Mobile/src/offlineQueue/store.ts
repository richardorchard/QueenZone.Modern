import { createMemoryStorage, type KeyValueStorage } from '../cache/storage';
import {
  OFFLINE_QUEUE_SCHEMA_VERSION,
  OFFLINE_QUEUE_STORAGE_KEY,
  type OfflineQueueItem,
} from './types';

let storage: KeyValueStorage | null = null;
const listeners = new Set<() => void>();

function getStorage(): KeyValueStorage {
  if (!storage) {
    // eslint-disable-next-line @typescript-eslint/no-require-imports -- lazy so Node unit tests never load React Native AsyncStorage.
    const loaded = require('../cache/asyncStorageAdapter') as typeof import('../cache/asyncStorageAdapter');
    storage = loaded.createAsyncStorageAdapter();
  }
  return storage;
}

export function setOfflineQueueStorageForTests(next: KeyValueStorage | null): void {
  storage = next ?? createMemoryStorage();
}

export function subscribeOfflineQueue(listener: () => void): () => void {
  listeners.add(listener);
  return () => listeners.delete(listener);
}

function notify(): void {
  for (const listener of listeners) {
    listener();
  }
}

function isItem(value: unknown): value is OfflineQueueItem {
  if (!value || typeof value !== 'object') {
    return false;
  }
  const row = value as OfflineQueueItem;
  return (
    row.schemaVersion === OFFLINE_QUEUE_SCHEMA_VERSION &&
    typeof row.operationId === 'string' &&
    typeof row.memberId === 'string' &&
    typeof row.kind === 'string' &&
    typeof row.payload?.body === 'string' &&
    (row.state === 'queued' || row.state === 'sending' || row.state === 'needs_attention')
  );
}

async function readAll(): Promise<OfflineQueueItem[]> {
  const raw = await getStorage().getItem(OFFLINE_QUEUE_STORAGE_KEY);
  if (!raw) {
    return [];
  }
  try {
    const parsed = JSON.parse(raw) as unknown;
    if (!Array.isArray(parsed)) {
      await getStorage().removeItem(OFFLINE_QUEUE_STORAGE_KEY);
      return [];
    }
    return parsed.filter(isItem);
  } catch {
    await getStorage().removeItem(OFFLINE_QUEUE_STORAGE_KEY);
    return [];
  }
}

async function writeAll(items: OfflineQueueItem[]): Promise<void> {
  await getStorage().setItem(OFFLINE_QUEUE_STORAGE_KEY, JSON.stringify(items));
  notify();
}

export async function listOfflineQueue(memberId?: string | null): Promise<OfflineQueueItem[]> {
  const items = await readAll();
  if (!memberId) {
    return items;
  }
  return items.filter((item) => item.memberId === memberId);
}

export async function enqueueOfflineItem(item: OfflineQueueItem): Promise<void> {
  const items = await readAll();
  const next = items.filter((row) => row.operationId !== item.operationId);
  next.push(item);
  next.sort((a, b) => a.createdAt.localeCompare(b.createdAt));
  await writeAll(next);
}

export async function updateOfflineItem(
  operationId: string,
  patch: Partial<OfflineQueueItem>,
): Promise<OfflineQueueItem | null> {
  const items = await readAll();
  const index = items.findIndex((row) => row.operationId === operationId);
  if (index < 0) {
    return null;
  }
  const updated = { ...items[index], ...patch, operationId, updatedAt: new Date().toISOString() };
  items[index] = updated;
  await writeAll(items);
  return updated;
}

export async function removeOfflineItem(operationId: string): Promise<void> {
  const items = await readAll();
  const next = items.filter((row) => row.operationId !== operationId);
  if (next.length === items.length) {
    return;
  }
  await writeAll(next);
}

export async function discardOfflineQueue(memberId?: string | null): Promise<void> {
  if (!memberId) {
    await getStorage().removeItem(OFFLINE_QUEUE_STORAGE_KEY);
    notify();
    return;
  }
  const items = await readAll();
  await writeAll(items.filter((row) => row.memberId !== memberId));
}

export async function countPendingOfflineItems(memberId?: string | null): Promise<number> {
  return (await listOfflineQueue(memberId)).length;
}
