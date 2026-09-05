import { createAsyncStorageAdapter } from '../cache/asyncStorageAdapter';
import type { KeyValueStorage } from '../cache/storage';
import { getDownloadFileHost } from './files';
import {
  DOWNLOAD_MANIFEST_SCHEMA_VERSION,
  type DownloadManifest,
  type DownloadManifestEntry,
} from './types';

const MANIFEST_KEY_PREFIX = 'qz:downloads:v1:member:';

function manifestKey(memberId: string): string {
  return `${MANIFEST_KEY_PREFIX}${memberId}`;
}

let storage: KeyValueStorage = createAsyncStorageAdapter();

export function setDownloadManifestStorageForTests(next: KeyValueStorage | null): void {
  storage = next ?? createAsyncStorageAdapter();
}

export function emptyManifest(memberId: string): DownloadManifest {
  return { schemaVersion: DOWNLOAD_MANIFEST_SCHEMA_VERSION, memberId, entries: {} };
}

function isEntry(value: unknown): value is DownloadManifestEntry {
  if (value === null || typeof value !== 'object') {
    return false;
  }
  const entry = value as DownloadManifestEntry;
  return (
    typeof entry.performanceId === 'string' &&
    typeof entry.localUri === 'string' &&
    typeof entry.title === 'string' &&
    typeof entry.performedBy === 'string' &&
    typeof entry.completedAt === 'string' &&
    typeof entry.memberId === 'string' &&
    (entry.byteSize === null || typeof entry.byteSize === 'number') &&
    (entry.sourceRevision === null || typeof entry.sourceRevision === 'string')
  );
}

function parseManifest(raw: string | null, memberId: string): DownloadManifest {
  if (!raw) {
    return emptyManifest(memberId);
  }

  try {
    const parsed = JSON.parse(raw) as Partial<DownloadManifest>;
    if (parsed.schemaVersion !== DOWNLOAD_MANIFEST_SCHEMA_VERSION || parsed.memberId !== memberId) {
      return emptyManifest(memberId);
    }

    const entries: Record<string, DownloadManifestEntry> = {};
    for (const [id, entry] of Object.entries(parsed.entries ?? {})) {
      if (isEntry(entry) && entry.performanceId === id && entry.memberId === memberId) {
        entries[id] = entry;
      }
    }
    return { schemaVersion: DOWNLOAD_MANIFEST_SCHEMA_VERSION, memberId, entries };
  } catch {
    return emptyManifest(memberId);
  }
}

export async function readDownloadManifest(memberId: string): Promise<DownloadManifest> {
  return parseManifest(await storage.getItem(manifestKey(memberId)), memberId);
}

export async function writeDownloadManifest(manifest: DownloadManifest): Promise<void> {
  await storage.setItem(manifestKey(manifest.memberId), JSON.stringify(manifest));
}

export async function getCompletedDownload(
  memberId: string,
  performanceId: string,
): Promise<DownloadManifestEntry | null> {
  const manifest = await readDownloadManifest(memberId);
  return manifest.entries[performanceId] ?? null;
}

export async function upsertCompletedDownload(entry: DownloadManifestEntry): Promise<void> {
  const manifest = await readDownloadManifest(entry.memberId);
  manifest.entries[entry.performanceId] = entry;
  await writeDownloadManifest(manifest);
}

export async function removeCompletedDownload(memberId: string, performanceId: string): Promise<void> {
  const manifest = await readDownloadManifest(memberId);
  delete manifest.entries[performanceId];
  await writeDownloadManifest(manifest);
}

export async function clearDownloadManifest(memberId?: string | null): Promise<void> {
  if (memberId) {
    await storage.removeItem(manifestKey(memberId));
    return;
  }

  const keys = await storage.getAllKeys();
  const ours = keys.filter((key) => key.startsWith(MANIFEST_KEY_PREFIX));
  if (ours.length > 0) {
    await storage.multiRemove(ours);
  }
}

/**
 * Drop missing/zero-length completed files and scrub leftover `.part` files.
 * Partial or failed downloads never become completed entries.
 */
export async function reconcileDownloadManifest(memberId: string): Promise<DownloadManifest> {
  const host = getDownloadFileHost();
  const manifest = await readDownloadManifest(memberId);
  let dirty = false;

  for (const [id, entry] of Object.entries(manifest.entries)) {
    const exists = host.exists(entry.localUri);
    const size = exists ? host.size(entry.localUri) : 0;
    if (!exists || size <= 0 || entry.memberId !== memberId) {
      delete manifest.entries[id];
      if (exists) {
        host.deleteIfExists(entry.localUri);
      }
      dirty = true;
    }
  }

  for (const partUri of host.listPartUris()) {
    host.deleteIfExists(partUri);
  }

  if (dirty) {
    await writeDownloadManifest(manifest);
  }

  return manifest;
}
