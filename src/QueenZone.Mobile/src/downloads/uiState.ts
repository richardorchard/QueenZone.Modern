import { downloadUiCacheKey, downloadUiCachePrefix, DOWNLOAD_UI_CACHE_KEY_PREFIX } from '../cache/keys';
import { invalidate, invalidatePrefix } from '../cache/externalStore';
import type { DownloadManifestEntry, DownloadUiSnapshot, DownloadUiStatus } from './types';

const snapshots = new Map<string, DownloadUiSnapshot>();

function key(memberId: string, performanceId: string): string {
  return downloadUiCacheKey(memberId, performanceId);
}

export function getDownloadUiSnapshot(memberId: string, performanceId: string): DownloadUiSnapshot | null {
  return snapshots.get(key(memberId, performanceId)) ?? null;
}

export function listDownloadUiSnapshots(memberId: string): DownloadUiSnapshot[] {
  const prefix = downloadUiCachePrefix(memberId);
  return [...snapshots.entries()]
    .filter(([cacheKey]) => cacheKey.startsWith(prefix))
    .map(([, snapshot]) => snapshot)
    .sort((a, b) => a.title.localeCompare(b.title));
}

export function setDownloadUiSnapshot(
  memberId: string,
  snapshot: DownloadUiSnapshot,
): DownloadUiSnapshot {
  snapshots.set(key(memberId, snapshot.performanceId), snapshot);
  invalidate(key(memberId, snapshot.performanceId));
  return snapshot;
}

export function clearDownloadUiSnapshot(memberId: string, performanceId: string): void {
  snapshots.delete(key(memberId, performanceId));
  invalidate(key(memberId, performanceId));
}

export function hydrateDownloadUiFromManifest(
  memberId: string,
  entries: DownloadManifestEntry[],
): void {
  const prefix = downloadUiCachePrefix(memberId);
  for (const cacheKey of [...snapshots.keys()]) {
    if (cacheKey.startsWith(prefix)) {
      snapshots.delete(cacheKey);
    }
  }
  for (const entry of entries) {
    snapshots.set(key(memberId, entry.performanceId), snapshotFromEntry(entry));
  }
  invalidatePrefix(prefix);
}

export function clearDownloadUiForMember(memberId?: string | null): void {
  if (memberId) {
    const prefix = downloadUiCachePrefix(memberId);
    for (const cacheKey of [...snapshots.keys()]) {
      if (cacheKey.startsWith(prefix)) {
        snapshots.delete(cacheKey);
      }
    }
    invalidatePrefix(prefix);
    return;
  }

  snapshots.clear();
  invalidatePrefix(DOWNLOAD_UI_CACHE_KEY_PREFIX);
}

export function snapshotFromEntry(entry: DownloadManifestEntry): DownloadUiSnapshot {
  return {
    status: 'downloaded',
    performanceId: entry.performanceId,
    title: entry.title,
    performedBy: entry.performedBy,
    byteSize: entry.byteSize,
    expectedBytes: entry.byteSize,
    error: null,
  };
}

export function transientSnapshot(
  performanceId: string,
  status: DownloadUiStatus,
  extras: Partial<DownloadUiSnapshot> = {},
): DownloadUiSnapshot {
  return {
    status,
    performanceId,
    title: extras.title ?? '',
    performedBy: extras.performedBy ?? '',
    byteSize: extras.byteSize ?? null,
    expectedBytes: extras.expectedBytes ?? null,
    error: extras.error ?? null,
  };
}

export function resetDownloadUiForTests(): void {
  snapshots.clear();
}
