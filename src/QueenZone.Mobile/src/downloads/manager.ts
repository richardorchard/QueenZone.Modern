import type { FanPerformance } from '../api';
import { apiV1Url } from '../config';
import { fanPerformanceAudioPath } from '../audio/formatDuration';
import { fileLooksLikeHttpError } from './audioBytes';
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
  DOWNLOAD_INCOMPLETE_MESSAGE,
  DOWNLOAD_RATE_LIMITED_MESSAGE,
  DOWNLOAD_TIMEOUT_MESSAGE,
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

const PROBE_TIMEOUT_MS = 12_000;
const DOWNLOAD_TIMEOUT_MS = 5 * 60 * 1000;
const PROGRESS_THROTTLE_MS = 200;

function mutexKey(memberId: string, performanceId: string): string {
  return `${memberId}:${performanceId}`;
}

function isJobActive(key: string): boolean {
  return inflight.has(key) || queuedTracks.has(key) || activeId === key;
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

async function cancelResponseBody(response: Response): Promise<void> {
  try {
    const body = response.body as { cancel?: () => Promise<void> } | null;
    if (body && typeof body.cancel === 'function') {
      await body.cancel();
    }
  } catch {
    // Never block a download on probe teardown.
  }
}

/**
 * Best-effort size/ETag probe. Range: bytes=0-0 must not download the whole
 * track — cancel the body immediately and abort if headers take too long.
 * A 200 (Range ignored by a proxy) is OK only after the body is cancelled.
 */
async function defaultProbeAudio(
  url: string,
  token: string,
): Promise<{ sourceRevision: string | null; byteSize: number | null; status: number }> {
  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), PROBE_TIMEOUT_MS);
  try {
    const response = await fetch(url, {
      headers: {
        Authorization: `Bearer ${token}`,
        Range: 'bytes=0-0',
      },
      signal: controller.signal,
    });
    await cancelResponseBody(response);
    const etag = response.headers.get('etag');
    const ranged = parseContentRangeTotal(response.headers.get('content-range'));
    const length = Number(response.headers.get('content-length'));
    return {
      status: response.status,
      sourceRevision: etag && etag.trim() ? etag.trim() : null,
      byteSize: ranged ?? (Number.isFinite(length) && length > 0 ? length : null),
    };
  } catch (error) {
    if (error instanceof Error && error.name === 'AbortError') {
      return { status: 0, sourceRevision: null, byteSize: null };
    }
    throw error;
  } finally {
    clearTimeout(timer);
  }
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
  if (current?.status === 'downloaded' || current?.status === 'removing') {
    return;
  }
  // Stale queued/downloading UI (hung job, force-quit leftover in-session) must
  // not ignore retry taps. Only skip when a job is actually running.
  if (isJobActive(key)) {
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

function messageForDownloadError(error: unknown): string {
  if (error instanceof Error && error.message) {
    if (
      error.message === DOWNLOAD_UNAUTHORIZED_MESSAGE ||
      error.message === LOW_STORAGE_MESSAGE ||
      error.message === DOWNLOAD_RATE_LIMITED_MESSAGE ||
      error.message === DOWNLOAD_TIMEOUT_MESSAGE ||
      error.message === DOWNLOAD_INCOMPLETE_MESSAGE ||
      error.message === DOWNLOAD_FAILED_MESSAGE
    ) {
      return error.message;
    }
    if (error.name === 'AbortError' || /aborted|timeout/i.test(error.message)) {
      return DOWNLOAD_TIMEOUT_MESSAGE;
    }
  }
  return DOWNLOAD_FAILED_MESSAGE;
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

  const downloadAbort = new AbortController();
  const downloadTimer = setTimeout(() => downloadAbort.abort(), DOWNLOAD_TIMEOUT_MS);
  let abortReason: 'timeout' | 'storage' | null = null;

  try {
    const token = await ensureAccessToken();
    if (!token) {
      throw new Error(DOWNLOAD_UNAUTHORIZED_MESSAGE);
    }

    const url = apiV1Url(fanPerformanceAudioPath(track.id));
    let sourceRevision: string | null = null;
    let expectedBytes: number | null = null;
    try {
      const probe = await probeAudio(url, token);
      if (probe.status === 401 || probe.status === 403 || probe.status === 404) {
        throw new Error(DOWNLOAD_UNAUTHORIZED_MESSAGE);
      }
      if (probe.status === 429) {
        throw new Error(DOWNLOAD_RATE_LIMITED_MESSAGE);
      }
      // Non-200/206 (including a timed-out probe status 0) is not fatal.
      // Streaming already proved the file is there; File.createDownloadTask
      // is the real transfer. A Range probe that ignores Range and buffers
      // the whole MP3 used to fail longer tracks and leak the connection.
      sourceRevision = probe.sourceRevision;
      expectedBytes = probe.byteSize;
    } catch (error) {
      if (error instanceof Error && error.message === DOWNLOAD_UNAUTHORIZED_MESSAGE) {
        throw error;
      }
      if (error instanceof Error && error.message === DOWNLOAD_RATE_LIMITED_MESSAGE) {
        throw error;
      }
      // Probe network errors: still attempt the download.
    }

    if (expectedBytes && host.availableBytes() < expectedBytes + DISK_SAFETY_MARGIN_BYTES) {
      throw new Error(LOW_STORAGE_MESSAGE);
    }

    setDownloadUiSnapshot(
      memberId,
      transientSnapshot(performanceId, 'downloading', {
        title: track.title,
        performedBy: track.performedBy,
        expectedBytes,
      }),
    );

    host.deleteIfExists(partUri);
    let lastProgressAt = 0;
    await host.download({
      url,
      destUri: partUri,
      headers: { Authorization: `Bearer ${token}` },
      signal: downloadAbort.signal,
      onProgress: (written, total) => {
        const knownTotal = total > 0 ? total : expectedBytes;
        if (knownTotal && host.availableBytes() < knownTotal + DISK_SAFETY_MARGIN_BYTES) {
          abortReason = 'storage';
          downloadAbort.abort();
          return;
        }
        const now = Date.now();
        if (now - lastProgressAt < PROGRESS_THROTTLE_MS && knownTotal != null && written < knownTotal) {
          return;
        }
        lastProgressAt = now;
        if (knownTotal && knownTotal > 0) {
          expectedBytes = knownTotal;
        }
        setDownloadUiSnapshot(
          memberId,
          transientSnapshot(performanceId, 'downloading', {
            title: track.title,
            performedBy: track.performedBy,
            byteSize: written,
            expectedBytes,
          }),
        );
      },
    });

    if (downloadAbort.signal.aborted) {
      throw new Error(abortReason === 'storage' ? LOW_STORAGE_MESSAGE : DOWNLOAD_TIMEOUT_MESSAGE);
    }

    const size = host.size(partUri);
    if (!host.exists(partUri) || size <= 0) {
      host.deleteIfExists(partUri);
      throw new Error(DOWNLOAD_FAILED_MESSAGE);
    }
    if (expectedBytes && expectedBytes > 64 && size < expectedBytes * 0.95) {
      host.deleteIfExists(partUri);
      throw new Error(DOWNLOAD_INCOMPLETE_MESSAGE);
    }
    if (await fileLooksLikeHttpError((uri, max) => host.readPrefix(uri, max), partUri, size)) {
      host.deleteIfExists(partUri);
      throw new Error(DOWNLOAD_FAILED_MESSAGE);
    }

    host.promote(partUri, completedUri);
    const byteSize = host.size(completedUri);
    if (
      !host.exists(completedUri) ||
      byteSize <= 0 ||
      (await fileLooksLikeHttpError((uri, max) => host.readPrefix(uri, max), completedUri, byteSize))
    ) {
      host.deleteIfExists(completedUri);
      throw new Error(DOWNLOAD_FAILED_MESSAGE);
    }

    const entry = {
      performanceId,
      localUri: completedUri,
      title: track.title,
      performedBy: track.performedBy,
      byteSize,
      sourceRevision,
      completedAt: new Date().toISOString(),
      memberId,
    };
    await upsertCompletedDownload(entry);
    setDownloadUiSnapshot(memberId, snapshotFromEntry(entry));
  } catch (error) {
    host.deleteIfExists(partUri);
    host.deleteIfExists(completedUri);
    const message = messageForDownloadError(error);
    setDownloadUiSnapshot(
      memberId,
      transientSnapshot(performanceId, 'failed', {
        title: track.title,
        performedBy: track.performedBy,
        error: message,
      }),
    );
  } finally {
    clearTimeout(downloadTimer);
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

export async function discardInvalidLocalDownload(memberId: string, performanceId: string): Promise<void> {
  const host = getDownloadFileHost();
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
