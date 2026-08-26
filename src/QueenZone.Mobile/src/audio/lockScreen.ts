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

export function lockScreenMetadata(track: Pick<FanPerformance, 'title' | 'performedBy'>): {
  title: string;
  artist: string;
  albumTitle: string;
} {
  return {
    title: track.title,
    artist: track.performedBy,
    albumTitle: lockScreenAlbumTitle,
  };
}
