import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from 'react';
import {
  setAudioModeAsync,
  useAudioPlayer,
  useAudioPlayerStatus,
} from 'expo-audio';
import { apiV1Url } from '../config';
import { useSession } from '../session/SessionContext';
import type { FanPerformance } from '../api';
import { fanPerformanceAudioPath } from './formatDuration';

type PlayerState = {
  current: FanPerformance | null;
  queue: FanPerformance[];
  error: string | null;
  playing: boolean;
  currentTime: number;
  duration: number;
  isLoaded: boolean;
  isBuffering: boolean;
  play: (track: FanPerformance, queue?: FanPerformance[]) => void;
  toggle: () => void;
  seekTo: (seconds: number) => void;
  skip: (deltaSeconds: number) => void;
  playNext: () => void;
  playPrevious: () => void;
};

const PlayerContext = createContext<PlayerState | undefined>(undefined);

export function FanPerformancePlayerProvider({ children }: { children: ReactNode }) {
  const { accessToken } = useSession();
  const player = useAudioPlayer(null, { updateInterval: 250 });
  const status = useAudioPlayerStatus(player);
  const [current, setCurrent] = useState<FanPerformance | null>(null);
  const [queue, setQueue] = useState<FanPerformance[]>([]);
  const [error, setError] = useState<string | null>(null);
  const currentRef = useRef<FanPerformance | null>(null);
  const queueRef = useRef<FanPerformance[]>([]);
  currentRef.current = current;
  queueRef.current = queue;

  useEffect(() => {
    setAudioModeAsync({
      playsInSilentMode: true,
      shouldPlayInBackground: true,
      interruptionMode: 'doNotMix',
    }).catch(() => {
      /* native audio session is unavailable in node tests */
    });
  }, []);

  const load = useCallback(
    (track: FanPerformance) => {
      if (!accessToken) {
        setError('Sign in with a member account to stream recordings.');
        return;
      }

      setError(null);
      setCurrent(track);
      player.replace({
        uri: apiV1Url(fanPerformanceAudioPath(track.id)),
        headers: { Authorization: `Bearer ${accessToken}` },
        name: track.title,
      });
      player.play();
      player.setActiveForLockScreen(
        true,
        {
          title: track.title,
          artist: track.performedBy,
          albumTitle: 'Fan performances',
        },
        { showSeekBackward: true, showSeekForward: true },
      );
    },
    [accessToken, player],
  );

  const play = useCallback(
    (track: FanPerformance, nextQueue: FanPerformance[] = []) => {
      setQueue(nextQueue.length > 0 ? nextQueue : [track]);
      load(track);
    },
    [load],
  );

  const playAdjacent = useCallback(
    (direction: 1 | -1) => {
      const list = queueRef.current;
      const playing = currentRef.current;
      if (!playing || list.length === 0) {
        return;
      }
      const index = list.findIndex((item) => item.id === playing.id);
      const next = list[index + direction];
      if (next) {
        load(next);
      }
    },
    [load],
  );

  const playNext = useCallback(() => playAdjacent(1), [playAdjacent]);
  const playPrevious = useCallback(() => playAdjacent(-1), [playAdjacent]);

  useEffect(() => {
    if (status.didJustFinish) {
      playNext();
    }
  }, [status.didJustFinish, playNext]);

  const toggle = useCallback(() => {
    if (!currentRef.current) {
      return;
    }
    if (status.playing) {
      player.pause();
      return;
    }
    player.play();
  }, [player, status.playing]);

  const seekTo = useCallback(
    (seconds: number) => {
      const duration = status.duration > 0 ? status.duration : currentRef.current?.durationSeconds ?? 0;
      const clamped = Math.max(0, Math.min(seconds, duration || seconds));
      player.seekTo(clamped);
    },
    [player, status.duration],
  );

  const skip = useCallback(
    (deltaSeconds: number) => {
      seekTo(status.currentTime + deltaSeconds);
    },
    [seekTo, status.currentTime],
  );

  const value = useMemo<PlayerState>(
    () => ({
      current,
      queue,
      error,
      playing: status.playing,
      currentTime: status.currentTime,
      duration: status.duration > 0 ? status.duration : (current?.durationSeconds ?? 0),
      isLoaded: status.isLoaded,
      isBuffering: status.isBuffering,
      play,
      toggle,
      seekTo,
      skip,
      playNext,
      playPrevious,
    }),
    [
      current,
      queue,
      error,
      status.playing,
      status.currentTime,
      status.duration,
      status.isLoaded,
      status.isBuffering,
      play,
      toggle,
      seekTo,
      skip,
      playNext,
      playPrevious,
    ],
  );

  return <PlayerContext.Provider value={value}>{children}</PlayerContext.Provider>;
}

export function useFanPerformancePlayer(): PlayerState {
  const value = useContext(PlayerContext);
  if (!value) {
    throw new Error('useFanPerformancePlayer must be used inside FanPerformancePlayerProvider');
  }

  return value;
}
