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
import { audioSessionMode, lockScreenMetadata, lockScreenOptions } from './lockScreen';

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
  const { accessToken, ensureAccessToken } = useSession();
  const player = useAudioPlayer(null, { updateInterval: 250 });
  const status = useAudioPlayerStatus(player);
  const [current, setCurrent] = useState<FanPerformance | null>(null);
  const [queue, setQueue] = useState<FanPerformance[]>([]);
  const [error, setError] = useState<string | null>(null);
  const currentRef = useRef<FanPerformance | null>(null);
  const queueRef = useRef<FanPerformance[]>([]);
  const loadGenerationRef = useRef(0);
  currentRef.current = current;
  queueRef.current = queue;

  useEffect(() => {
    setAudioModeAsync({ ...audioSessionMode }).catch(() => {
      /* native audio session is unavailable in node tests */
    });
  }, []);

  const clearNowPlaying = useCallback(() => {
    player.pause();
    player.clearLockScreenControls();
    setCurrent(null);
  }, [player]);

  const load = useCallback(
    (track: FanPerformance) => {
      const generation = ++loadGenerationRef.current;
      void (async () => {
        const token = await ensureAccessToken();
        if (generation !== loadGenerationRef.current) {
          return;
        }

        if (!token) {
          setError('Sign in with a member account to stream recordings.');
          return;
        }

        setError(null);
        setCurrent(track);
        player.replace({
          uri: apiV1Url(fanPerformanceAudioPath(track.id)),
          headers: { Authorization: `Bearer ${token}` },
          name: track.title,
        });
        player.setActiveForLockScreen(true, lockScreenMetadata(track), { ...lockScreenOptions });
        player.play();
      })();
    },
    [ensureAccessToken, player],
  );

  const play = useCallback(
    (track: FanPerformance, nextQueue: FanPerformance[] = []) => {
      setQueue(nextQueue.length > 0 ? nextQueue : [track]);
      load(track);
    },
    [load],
  );

  const playAdjacent = useCallback(
    (direction: 1 | -1): boolean => {
      const list = queueRef.current;
      const playing = currentRef.current;
      if (!playing || list.length === 0) {
        return false;
      }
      const index = list.findIndex((item) => item.id === playing.id);
      const next = list[index + direction];
      if (!next) {
        return false;
      }
      load(next);
      return true;
    },
    [load],
  );

  const playNext = useCallback(() => {
    playAdjacent(1);
  }, [playAdjacent]);
  const playPrevious = useCallback(() => {
    playAdjacent(-1);
  }, [playAdjacent]);

  useEffect(() => {
    if (!status.didJustFinish) {
      return;
    }
    if (!playAdjacent(1)) {
      clearNowPlaying();
    }
  }, [status.didJustFinish, playAdjacent, clearNowPlaying]);

  useEffect(() => {
    if (accessToken) {
      return;
    }
    if (!currentRef.current && queueRef.current.length === 0) {
      return;
    }
    player.pause();
    player.clearLockScreenControls();
    setCurrent(null);
    setQueue([]);
    setError(null);
  }, [accessToken, player]);

  useEffect(() => {
    return () => {
      player.clearLockScreenControls();
    };
  }, [player]);

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
