import { createMemoryStorage } from '../cache/storage';
import { resetExternalStoreForTests } from '../cache/externalStore';
import { fanPerformanceFixture } from '../test/fixtures';
import { createMemoryDownloadHost, setDownloadFileHostForTests } from './files';
import {
  getCompletedDownload,
  reconcileDownloadManifest,
  removeCompletedDownload,
  setDownloadManifestStorageForTests,
  upsertCompletedDownload,
} from './manifest';
import {
  enqueueDownload,
  purgeAllDownloads,
  removeDownload,
  resetDownloadManagerForTests,
  setDownloadProbeForTests,
} from './manager';
import {
  DOWNLOAD_RATE_LIMITED_MESSAGE,
  OFFLINE_PLAYBACK_MESSAGE,
  SIGN_IN_PLAYBACK_MESSAGE,
} from './messages';
import { resolveAudioSource } from './resolveAudioSource';
import { getDownloadUiSnapshot, resetDownloadUiForTests, setDownloadUiSnapshot, transientSnapshot } from './uiState';
import type { DownloadManifestEntry } from './types';

const memberId = 'member-1';
const track = fanPerformanceFixture();

function completed(overrides: Partial<DownloadManifestEntry> = {}): DownloadManifestEntry {
  return {
    performanceId: '187',
    localUri: 'file:///documents/fan-performances/187',
    title: track.title,
    performedBy: track.performedBy,
    byteSize: 4,
    sourceRevision: '"etag-1"',
    completedAt: '2026-09-05T00:00:00.000Z',
    memberId,
    ...overrides,
  };
}

function resetDownloads() {
  resetDownloadManagerForTests();
  resetDownloadUiForTests();
  resetExternalStoreForTests();
  setDownloadManifestStorageForTests(createMemoryStorage());
  setDownloadFileHostForTests(createMemoryDownloadHost());
  setDownloadProbeForTests(null);
}

describe('download manifest reconciliation', () => {
  beforeEach(resetDownloads);
  afterEach(() => {
    setDownloadFileHostForTests(null);
    setDownloadManifestStorageForTests(null);
  });

  it('drops missing and zero-length files and scrubs orphan parts', async () => {
    const host = createMemoryDownloadHost();
    setDownloadFileHostForTests(host);
    host.files.set('file:///documents/fan-performances/188', new Uint8Array());
    host.files.set('file:///documents/fan-performances/189.part', new Uint8Array([1]));
    await upsertCompletedDownload(completed());
    await upsertCompletedDownload(
      completed({
        performanceId: '188',
        localUri: 'file:///documents/fan-performances/188',
      }),
    );

    const next = await reconcileDownloadManifest(memberId);
    expect(next.entries['187']).toBeUndefined();
    expect(next.entries['188']).toBeUndefined();
    expect(host.exists('file:///documents/fan-performances/189.part')).toBe(false);
    expect(await getCompletedDownload(memberId, '187')).toBeNull();
  });

  it('keeps a valid completed file until it is removed', async () => {
    const host = createMemoryDownloadHost();
    setDownloadFileHostForTests(host);
    host.files.set('file:///documents/fan-performances/187', new Uint8Array([1, 2, 3, 4]));
    await upsertCompletedDownload(completed());

    const kept = await reconcileDownloadManifest(memberId);
    expect(kept.entries['187']?.sourceRevision).toBe('"etag-1"');
    await removeCompletedDownload(memberId, '187');
    expect(await getCompletedDownload(memberId, '187')).toBeNull();
  });
});

describe('resolveAudioSource', () => {
  beforeEach(resetDownloads);
  afterEach(() => {
    setDownloadFileHostForTests(null);
    setDownloadManifestStorageForTests(null);
  });

  it('prefers a valid same-member local file without refreshing a token', async () => {
    const host = createMemoryDownloadHost();
    setDownloadFileHostForTests(host);
    host.files.set('file:///documents/fan-performances/187', new Uint8Array([1, 2, 3, 4]));
    await upsertCompletedDownload(completed());
    const ensureAccessToken = jest.fn(async () => 'should-not-run');

    await expect(
      resolveAudioSource({
        track,
        memberId,
        ensureAccessToken,
        isOffline: true,
      }),
    ).resolves.toEqual({ kind: 'local', uri: 'file:///documents/fan-performances/187' });
    expect(ensureAccessToken).not.toHaveBeenCalled();
  });

  it('returns the offline-specific error when the recording is not downloaded', async () => {
    await expect(
      resolveAudioSource({
        track,
        memberId,
        ensureAccessToken: async () => 'token',
        isOffline: true,
      }),
    ).resolves.toEqual({ kind: 'error', message: OFFLINE_PLAYBACK_MESSAGE });
  });

  it('streams with Bearer when online and nothing is downloaded', async () => {
    const source = await resolveAudioSource({
      track,
      memberId,
      ensureAccessToken: async () => 'member-token',
      isOffline: false,
    });
    expect(source).toEqual({
      kind: 'stream',
      uri: expect.stringContaining('/content/fan-performances/187/audio'),
      headers: { Authorization: 'Bearer member-token' },
    });
    expect(JSON.stringify(source)).not.toContain('cdn');
    expect(JSON.stringify(source)).not.toContain('blob');
  });

  it('asks the member to sign in when there is no local file and no token', async () => {
    await expect(
      resolveAudioSource({
        track,
        memberId: null,
        ensureAccessToken: async () => null,
        isOffline: false,
      }),
    ).resolves.toEqual({ kind: 'error', message: SIGN_IN_PLAYBACK_MESSAGE });
  });

  it('discards a JSON error-page local file and streams instead', async () => {
    const host = createMemoryDownloadHost();
    setDownloadFileHostForTests(host);
    host.files.set(
      'file:///documents/fan-performances/187',
      new TextEncoder().encode('{"title":"Unauthorized"}'),
    );
    await upsertCompletedDownload(completed({ byteSize: 24 }));

    const source = await resolveAudioSource({
      track,
      memberId,
      ensureAccessToken: async () => 'member-token',
      isOffline: false,
    });
    expect(source).toEqual({
      kind: 'stream',
      uri: expect.stringContaining('/content/fan-performances/187/audio'),
      headers: { Authorization: 'Bearer member-token' },
    });
    expect(await getCompletedDownload(memberId, '187')).toBeNull();
    expect(host.exists('file:///documents/fan-performances/187')).toBe(false);
  });
});

describe('download manager', () => {
  beforeEach(resetDownloads);
  afterEach(() => {
    setDownloadFileHostForTests(null);
    setDownloadManifestStorageForTests(null);
    setDownloadProbeForTests(null);
  });

  it('prevents duplicate downloads from repeated taps and stores the ETag as sourceRevision', async () => {
    const host = createMemoryDownloadHost();
    setDownloadFileHostForTests(host);
    setDownloadProbeForTests(async () => ({
      status: 206,
      sourceRevision: '"etag-9"',
      byteSize: 4,
    }));

    enqueueDownload(track, memberId, async () => 'member-token');
    enqueueDownload(track, memberId, async () => 'member-token');
    await Promise.resolve();
    await Promise.resolve();
    await new Promise((resolve) => setTimeout(resolve, 0));

    expect(host.files.get('file:///documents/fan-performances/187')?.byteLength).toBe(4);
    expect(host.exists('file:///documents/fan-performances/187.part')).toBe(false);
    const stored = await getCompletedDownload(memberId, '187');
    expect(stored?.sourceRevision).toBe('"etag-9"');
    expect(getDownloadUiSnapshot(memberId, '187')?.status).toBe('downloaded');
  });

  it('rejects a completed file that is an HTTP error payload', async () => {
    const host = createMemoryDownloadHost({
      downloadImpl: async ({ destUri }) => {
        host.files.set(destUri, new TextEncoder().encode('{"title":"Unauthorized"}'));
      },
    });
    setDownloadFileHostForTests(host);
    setDownloadProbeForTests(async () => ({
      status: 206,
      sourceRevision: '"etag-9"',
      byteSize: 24,
    }));

    enqueueDownload(track, memberId, async () => 'member-token');
    await new Promise((resolve) => setTimeout(resolve, 0));
    expect(await getCompletedDownload(memberId, '187')).toBeNull();
    expect(host.exists('file:///documents/fan-performances/187')).toBe(false);
    expect(getDownloadUiSnapshot(memberId, '187')?.status).toBe('failed');
  });

  it('deletes the partial and does not complete after a failed download', async () => {
    const host = createMemoryDownloadHost({
      downloadImpl: async () => {
        throw new Error('boom');
      },
    });
    setDownloadFileHostForTests(host);
    setDownloadProbeForTests(async () => ({
      status: 206,
      sourceRevision: '"etag-9"',
      byteSize: 4,
    }));

    enqueueDownload(track, memberId, async () => 'member-token');
    await new Promise((resolve) => setTimeout(resolve, 0));

    expect(await getCompletedDownload(memberId, '187')).toBeNull();
    expect(host.exists('file:///documents/fan-performances/187.part')).toBe(false);
    expect(getDownloadUiSnapshot(memberId, '187')?.status).toBe('failed');
  });

  it('rejects unauthorized recordings and low storage without a completed entry', async () => {
    const host = createMemoryDownloadHost({ availableBytes: 100 });
    setDownloadFileHostForTests(host);
    setDownloadProbeForTests(async () => ({
      status: 404,
      sourceRevision: null,
      byteSize: null,
    }));

    enqueueDownload(track, memberId, async () => 'member-token');
    await new Promise((resolve) => setTimeout(resolve, 0));
    expect(await getCompletedDownload(memberId, '187')).toBeNull();
    expect(getDownloadUiSnapshot(memberId, '187')?.status).toBe('failed');
  });

  it('removes the file and manifest entry without touching a server copy', async () => {
    const host = createMemoryDownloadHost();
    setDownloadFileHostForTests(host);
    host.files.set('file:///documents/fan-performances/187', new Uint8Array([1, 2, 3, 4]));
    await upsertCompletedDownload(completed());

    await removeDownload(memberId, '187');
    expect(host.exists('file:///documents/fan-performances/187')).toBe(false);
    expect(await getCompletedDownload(memberId, '187')).toBeNull();
    expect(getDownloadUiSnapshot(memberId, '187')).toBeNull();
  });

  it('wires onProgress into the downloading snapshot', async () => {
    let release!: () => void;
    const held = new Promise<void>((resolve) => {
      release = resolve;
    });
    const host = createMemoryDownloadHost({
      downloadImpl: async ({ destUri, onProgress }) => {
        onProgress?.(256, 1024);
        await held;
        host.files.set(destUri, new Uint8Array(1024));
      },
    });
    setDownloadFileHostForTests(host);
    setDownloadProbeForTests(async () => ({
      status: 206,
      sourceRevision: '"etag-9"',
      byteSize: 1024,
    }));

    enqueueDownload(track, memberId, async () => 'member-token');
    await new Promise((resolve) => setTimeout(resolve, 0));
    const snapshot = getDownloadUiSnapshot(memberId, '187');
    expect(snapshot?.status).toBe('downloading');
    expect(snapshot?.byteSize).toBe(256);
    expect(snapshot?.expectedBytes).toBe(1024);

    release();
    await new Promise((resolve) => setTimeout(resolve, 0));
    expect(getDownloadUiSnapshot(memberId, '187')?.status).toBe('downloaded');
  });

  it('still downloads when the Range probe times out or returns a non-206', async () => {
    const host = createMemoryDownloadHost();
    setDownloadFileHostForTests(host);
    setDownloadProbeForTests(async () => ({
      status: 0,
      sourceRevision: null,
      byteSize: null,
    }));

    enqueueDownload(track, memberId, async () => 'member-token');
    await new Promise((resolve) => setTimeout(resolve, 0));

    expect(host.files.get('file:///documents/fan-performances/187')?.byteLength).toBe(4);
    expect(getDownloadUiSnapshot(memberId, '187')?.status).toBe('downloaded');
  });

  it('surfaces a 429 probe as a readable failed reason and stays retryable', async () => {
    const host = createMemoryDownloadHost();
    setDownloadFileHostForTests(host);
    setDownloadProbeForTests(async () => ({
      status: 429,
      sourceRevision: null,
      byteSize: null,
    }));

    enqueueDownload(track, memberId, async () => 'member-token');
    await new Promise((resolve) => setTimeout(resolve, 0));
    expect(getDownloadUiSnapshot(memberId, '187')).toMatchObject({
      status: 'failed',
      error: DOWNLOAD_RATE_LIMITED_MESSAGE,
    });

    setDownloadProbeForTests(async () => ({
      status: 206,
      sourceRevision: '"etag-9"',
      byteSize: 4,
    }));
    enqueueDownload(track, memberId, async () => 'member-token');
    await new Promise((resolve) => setTimeout(resolve, 0));
    expect(getDownloadUiSnapshot(memberId, '187')?.status).toBe('downloaded');
  });

  it('retries a stale downloading snapshot that has no running job', async () => {
    const host = createMemoryDownloadHost();
    setDownloadFileHostForTests(host);
    setDownloadProbeForTests(async () => ({
      status: 206,
      sourceRevision: '"etag-9"',
      byteSize: 4,
    }));
    setDownloadUiSnapshot(
      memberId,
      transientSnapshot('187', 'downloading', {
        title: track.title,
        performedBy: track.performedBy,
      }),
    );

    enqueueDownload(track, memberId, async () => 'member-token');
    await new Promise((resolve) => setTimeout(resolve, 0));
    expect(getDownloadUiSnapshot(memberId, '187')?.status).toBe('downloaded');
  });

  it('sign-out deletes files, partials, and the manifest', async () => {
    const host = createMemoryDownloadHost();
    setDownloadFileHostForTests(host);
    host.files.set('file:///documents/fan-performances/187', new Uint8Array([1, 2, 3, 4]));
    host.files.set('file:///documents/fan-performances/188.part', new Uint8Array([9]));
    await upsertCompletedDownload(completed());

    await purgeAllDownloads(memberId);
    expect(host.exists('file:///documents/fan-performances/187')).toBe(false);
    expect(host.exists('file:///documents/fan-performances/188.part')).toBe(false);
    expect(await getCompletedDownload(memberId, '187')).toBeNull();
  });
});
