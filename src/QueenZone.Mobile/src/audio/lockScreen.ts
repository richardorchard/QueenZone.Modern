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
 * Accept only an absolute `file:` URI for now-playing art. Scheme-less names
 * (`assets_icon`) and `asset:` paths throw on Android lock-screen
 * (QUEENZONE-MOBILE-9). Remote and blob URLs stay off metadata so tokens and
 * songfiles never reach the system player.
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
  if (lower.startsWith('file:')) {
    return trimmed;
  }

  return undefined;
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
