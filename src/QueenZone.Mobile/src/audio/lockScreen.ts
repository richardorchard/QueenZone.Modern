import type { FanPerformance } from '../api';

export const audioSessionMode = {
  playsInSilentMode: true,
  shouldPlayInBackground: true,
  interruptionMode: 'doNotMix',
} as const;

export const lockScreenOptions = {
  showSeekBackward: true,
  showSeekForward: true,
} as const;

export const lockScreenAlbumTitle = 'Fan performances';

export type LockScreenMetadata = {
  title: string;
  artist: string;
  albumTitle: string;
  artworkUrl?: string;
};

/**
 * Accept only a local file/asset URI for now-playing art. Remote and blob
 * URLs must never reach lock-screen metadata (tokens and songfiles stay off
 * the system player).
 */
export function lockScreenArtworkUrlOrOmit(url: string | undefined): string | undefined {
  if (!url) {
    return undefined;
  }

  const trimmed = url.trim();
  if (!trimmed) {
    return undefined;
  }

  const lower = trimmed.toLowerCase();
  if (lower.startsWith('https://') || lower.startsWith('http://') || lower.startsWith('blob:')) {
    return undefined;
  }

  return trimmed;
}

export function lockScreenMetadata(
  track: Pick<FanPerformance, 'title' | 'performedBy'>,
  artworkUrl?: string,
): LockScreenMetadata {
  const artwork = lockScreenArtworkUrlOrOmit(artworkUrl);
  return {
    title: track.title,
    artist: track.performedBy,
    albumTitle: lockScreenAlbumTitle,
    ...(artwork ? { artworkUrl: artwork } : {}),
  };
}
