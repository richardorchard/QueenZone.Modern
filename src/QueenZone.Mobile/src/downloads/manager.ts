import type { FanPerformance } from '../api';
import { apiV1Url } from '../config';
import { fanPerformanceAudioPath } from '../audio/formatDuration';
import { getDownloadFileHost } from './files';
import {
  clearDownloadManifest,
  readDownloadManifest,
  reconcileDownloadManifest,
  removeCompletedDownload,
  upsertCompletedDownload,
} from './manifest';
import {
  DOWNLOAD_FAILED_MESSAGE,
  DOWNLOAD_UNAUTHORIZED_MESSAGE,
  LOW_STORAGE_MESSAGE,
} from './messages';
import { stopActivePlayback, stopPlaybackIf } from './playbackStop';
import {
  clearDownloadUiForMember,
  clearDownloadUiSnapshot,
  getDownloadUiSnapshot,
  hydrateDownloadUiFromManifest,
  setDownloadUiSnapshot,
  snapshotFromEntry,
  transientSnapshot,
} from './uiState';
import { DISK_SAFETY_MARGIN_BYTES } from './types';

const inflight = new Map<string, Promise<void>>();
const queuedIds: string[] = [];
const queuedTracks = new Map<string, { track: FanPerformance; memberId: string; tokenFactory: () => Promise<string | null> }>();
let activeId: string | null = null;
let probeAudio: typeof defaultProbeAudio = defaultProbeAudio;

function mutexKey(memberId: string, performanceId: string): string {
  return `${memberId}:${performanceId}`;
}

function parseContentRangeTotal(header: string | null): number | null {
  if (!header) {
    return null;
  }
  const match = /\/(\d+)\s*$/.exec(header);
  if (!match?.[1]) {
    return null;
  }
  const total = Number(match[1]);
  return Number.isFinite(total) && total > 0 ? total : null;
}

async function defaultProbeAudio(url: string, token: string): Promise<{ sourceRevision: string | null; byteSize: number | null; status: number }> {
  const response = await fetch(url, {
    headers: {
      Authorization: `Bearer ${token}`,
      Range: 'bytes=0-0',
    },
  });
  const etag = response.headers.get('etag');
  const ranged = parseContentRangeTotal(response.headers.get('content-range'));
  const length = Number(response.headers.get('content-length'));
  return {
    status: response.status,
    sourceRevision: etag && etag.trim() ? etag.trim() : null,
    byteSize: ranged ?? (Number.isFinite(length) && length > 0 ? length : null),
  };
}

export function setDownloadProbeForTests(next: typeof defaultProbeAudio | null): void {
  probeAudio = next ?? defaultProbeAudio;
}

export async function reconcileDownloads(memberId: string): Promise<void> {
  const manifest = await reconcileDownloadManifest(memberId);
  hydrateDownloadUiFromManifest(memberId, Object.values(manifest.entries));
}

export function enqueueDownload(
  track: FanPerformance,
  memberId: string,
  ensureAccessToken: () => Promise<string | null>,
): void {
  const performanceId = String(track.id);
  const key = mutexKey(memberId, performanceId);
  const current = getDownloadUiSnapshot(memberId, performanceId);
  if (
    inflight.has(key) ||
    queuedTracks.has(key) ||
    activeId === key ||
    current?.status === 'queued' ||
    current?.status === 'downloading' ||
    current?.status === 'downloaded' ||
    current?.status === 'removing'
  ) {
    return;
  }

  queuedTracks.set(key, { track, memberId, tokenFactory: ensureAccessToken });
  queuedIds.push(key);
  setDownloadUiSnapshot(
    memberId,
    transientSnapshot(performanceId, 'queued', {
      title: track.title,
      performedBy: track.performedBy,
    }),
  );
  void pumpQueue();
}

async function pumpQueue(): Promise<void> {
  if (activeId) {
    return;
  }

  const key = queuedIds.shift();
  if (!key) {
    return;
  }

  const job = queuedTracks.get(key);
  queuedTracks.delete(key);
  if (!job) {
    void pumpQueue();
    return;
  }

  activeId = key;
  const work = runDownload(job.track, job.memberId, job.tokenFactory).finally(() => {
    inflight.delete(key);
    if (activeId === key) {
      activeId = null;
    }
    void pumpQueue();
  });
  inflight.set(key, work);
  await work;
}

async function runDownload(
  track: FanPerformance,
  memberId: string,
  ensureAccessToken: () => Promise<string | null>,
): Promise<void> {
  const performanceId = String(track.id);
  const host = getDownloadFileHost();
  const partUri = host.partUri(performanceId);
  const completedUri = host.completedUri(performanceId);

  setDownloadUiSnapshot(
    memberId,
    transientSnapshot(performanceId, 'downloading', {
      title: track.title,
      performedBy: track.performedBy,
    }),
  );

  try {
    const token = await ensureAccessToken();
    if (!token) {
      throw new Error(DOWNLOAD_UNAUTHORIZED_MESSAGE);
    }

    const url = apiV1Url(fanPerformanceAudioPath(track.id));
    const probe = await probeAudio(url, token);
    if (probe.status === 401 || probe.status === 403 || probe.status === 404) {
      throw new Error(DOWNLOAD_UNAUTHORIZED_MESSAGE);
    }
    if (probe.status !== 200 && probe.status !== 206) {
      throw new Error(DOWNLOAD_FAILED_MESSAGE);
    }

    if (probe.byteSize && host.availableBytes() < probe.byteSize + DISK_SAFETY_MARGIN_BYTES) {
      throw new Error(LOW_STORAGE_MESSAGE);
    }

    setDownloadUiSnapshot(
      memberId,
      transientSnapshot(performanceId, 'downloading', {
        title: track.title,
        performedBy: track.performedBy,
        expectedBytes: probe.byteSize,
      }),
    );

    host.deleteIfExists(partUri);
    await host.download({
      url,
      destUri: partUri,
      headers: { Authorization: `Bearer ${token}` },
    });

    const size = host.size(partUri);
    if (!host.exists(partUri) || size <= 0) {
      host.deleteIfExists(partUri);
      throw new Error(DOWNLOAD_FAILED_MESSAGE);
    }

    host.promote(partUri, completedUri);
    const byteSize = host.size(completedUri);
    if (!host.exists(completedUri) || byteSize <= 0) {
      host.deleteIfExists(completedUri);
      throw new Error(DOWNLOAD_FAILED_MESSAGE);
    }

    const entry = {
      performanceId,
      localUri: completedUri,
      title: track.title,
      performedBy: track.performedBy,
      byteSize,
      sourceRevision: probe.sourceRevision,
      completedAt: new Date().toISOString(),
      memberId,
    };
    await upsertCompletedDownload(entry);
    setDownloadUiSnapshot(memberId, snapshotFromEntry(entry));
  } catch (error) {
    host.deleteIfExists(partUri);
    const message = error instanceof Error && error.message ? error.message : DOWNLOAD_FAILED_MESSAGE;
    setDownloadUiSnapshot(
      memberId,
      transientSnapshot(performanceId, 'failed', {
        title: track.title,
        performedBy: track.performedBy,
        error: message,
      }),
    );
  }
}

export async function removeDownload(memberId: string, performanceId: string): Promise<void> {
  const host = getDownloadFileHost();
  setDownloadUiSnapshot(
    memberId,
    transientSnapshot(performanceId, 'removing', {
      title: getTitleHint(memberId, performanceId),
    }),
  );
  stopPlaybackIf(performanceId);
  host.deleteIfExists(host.completedUri(performanceId));
  host.deleteIfExists(host.partUri(performanceId));
  await removeCompletedDownload(memberId, performanceId);
  clearDownloadUiSnapshot(memberId, performanceId);
}

export async function purgeAllDownloads(memberId?: string | null): Promise<void> {
  stopActivePlayback();
  const host = getDownloadFileHost();
  if (memberId) {
    const manifest = await readDownloadManifest(memberId);
    for (const entry of Object.values(manifest.entries)) {
      host.deleteIfExists(entry.localUri);
      host.deleteIfExists(host.partUri(entry.performanceId));
    }
  } else {
    for (const uri of host.listAllUris()) {
      host.deleteIfExists(uri);
    }
  }

  for (const partUri of host.listPartUris()) {
    host.deleteIfExists(partUri);
  }

  await clearDownloadManifest(memberId);
  clearDownloadUiForMember(memberId);
  queuedIds.length = 0;
  queuedTracks.clear();
  inflight.clear();
  activeId = null;
}

function getTitleHint(memberId: string, performanceId: string): string {
  return getDownloadUiSnapshot(memberId, performanceId)?.title ?? '';
}

export function resetDownloadManagerForTests(): void {
  queuedIds.length = 0;
  queuedTracks.clear();
  inflight.clear();
  activeId = null;
  probeAudio = defaultProbeAudio;
}
