import type { FanPerformance } from '../api';
import { apiV1Url } from '../config';
import { fanPerformanceAudioPath } from '../audio/formatDuration';
import { fileLooksLikeHttpError } from './audioBytes';
import { getDownloadFileHost } from './files';
import { getCompletedDownload } from './manifest';
import { discardInvalidLocalDownload } from './manager';
import { OFFLINE_PLAYBACK_MESSAGE, SIGN_IN_PLAYBACK_MESSAGE } from './messages';

export type ResolvedAudioSource =
  | { kind: 'local'; uri: string }
  | { kind: 'stream'; uri: string; headers: Record<string, string> }
  | { kind: 'error'; message: string };

export type ResolveAudioSourceInput = {
  track: FanPerformance;
  memberId: string | null;
  ensureAccessToken: () => Promise<string | null>;
  isOffline: boolean;
  ignoreLocal?: boolean;
};

export async function hasValidLocalDownload(
  memberId: string,
  performanceId: string,
): Promise<{ uri: string } | null> {
  const entry = await getCompletedDownload(memberId, performanceId);
  if (!entry || entry.memberId !== memberId) {
    return null;
  }

  const host = getDownloadFileHost();
  const exists = host.exists(entry.localUri);
  const size = exists ? host.size(entry.localUri) : 0;
  const corrupt =
    !exists ||
    size <= 0 ||
    (await fileLooksLikeHttpError((uri, max) => host.readPrefix(uri, max), entry.localUri, size));
  if (corrupt) {
    await discardInvalidLocalDownload(memberId, performanceId);
    return null;
  }

  return { uri: entry.localUri };
}

/**
 * Prefer a valid same-member local file. Streaming needs a live Bearer token.
 * Local playback uses retained member identity only — no token refresh.
 * Missing, empty, or error-page local files are discarded so they cannot
 * wedge the shared player; playback then falls through to stream.
 */
export async function resolveAudioSource(input: ResolveAudioSourceInput): Promise<ResolvedAudioSource> {
  const performanceId = String(input.track.id);
  if (input.memberId && !input.ignoreLocal) {
    const local = await hasValidLocalDownload(input.memberId, performanceId);
    if (local) {
      return { kind: 'local', uri: local.uri };
    }
  }

  if (input.isOffline) {
    return { kind: 'error', message: OFFLINE_PLAYBACK_MESSAGE };
  }

  const token = await input.ensureAccessToken();
  if (!token) {
    return { kind: 'error', message: SIGN_IN_PLAYBACK_MESSAGE };
  }

  return {
    kind: 'stream',
    uri: apiV1Url(fanPerformanceAudioPath(input.track.id)),
    headers: { Authorization: `Bearer ${token}` },
  };
}
