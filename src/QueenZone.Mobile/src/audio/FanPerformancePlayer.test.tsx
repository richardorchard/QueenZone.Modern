import { Pressable, Text } from 'react-native';
import { screen, userEvent } from '@testing-library/react-native';
import { setAudioModeAsync } from 'expo-audio';
import { createMockSession } from '../test/mockSession';
import { fanPerformanceFixture } from '../test/fixtures';
import { renderWithProviders } from '../test/render';
import { lockScreenAlbumTitle, lockScreenMetadata, lockScreenOptions } from './lockScreen';
import { FanPerformancePlayerProvider, useFanPerformancePlayer } from './FanPerformancePlayer';

const mockPlayer = {
  replace: jest.fn(),
  play: jest.fn(),
  pause: jest.fn(),
  seekTo: jest.fn(),
  setActiveForLockScreen: jest.fn(),
  clearLockScreenControls: jest.fn(),
};

const mockStatus = {
  playing: false,
  currentTime: 0,
  duration: 0,
  isLoaded: true,
  isBuffering: false,
  didJustFinish: false,
};

const mockSession = createMockSession();

jest.mock('expo-audio', () => ({
  setAudioModeAsync: jest.fn(() => Promise.resolve()),
  useAudioPlayer: () => mockPlayer,
  useAudioPlayerStatus: () => ({ ...mockStatus }),
}));

jest.mock('../session/SessionContext', () => ({
  useSession: () => mockSession,
}));

jest.mock('../config', () => ({
  apiV1Url: (path: string) => `http://qz.test/api/v1${path.startsWith('/') ? path : `/${path}`}`,
}));

const trackA = fanPerformanceFixture();
const trackB = fanPerformanceFixture({
  id: 188,
  title: 'Radio Ga Ga',
  performedBy: 'Sam',
  detailPath: '/fan-performances/188',
  audioPath: '/api/v1/content/fan-performances/188/audio',
});

function Probe() {
  const player = useFanPerformancePlayer();
  return (
    <>
      <Text testID="current">{player.current?.title ?? 'none'}</Text>
      <Text testID="error">{player.error ?? 'no-error'}</Text>
      <Text testID="queue">{String(player.queue.length)}</Text>
      <Pressable testID="play-a" onPress={() => player.play(trackA, [trackA, trackB])}>
        <Text>play-a</Text>
      </Pressable>
      <Pressable testID="play-last" onPress={() => player.play(trackB, [trackA, trackB])}>
        <Text>play-last</Text>
      </Pressable>
      <Pressable testID="toggle" onPress={() => player.toggle()}>
        <Text>toggle</Text>
      </Pressable>
      <Pressable testID="next" onPress={() => player.playNext()}>
        <Text>next</Text>
      </Pressable>
      <Pressable testID="previous" onPress={() => player.playPrevious()}>
        <Text>previous</Text>
      </Pressable>
      <Pressable testID="seek" onPress={() => player.seekTo(12)}>
        <Text>seek</Text>
      </Pressable>
      <Pressable testID="skip" onPress={() => player.skip(15)}>
        <Text>skip</Text>
      </Pressable>
    </>
  );
}

function renderPlayer() {
  return renderWithProviders(
    <FanPerformancePlayerProvider>
      <Probe />
    </FanPerformancePlayerProvider>,
    { navigation: false },
  );
}

describe('FanPerformancePlayerProvider', () => {
  beforeEach(() => {
    mockSession.accessToken = 'member-token';
    mockStatus.playing = false;
    mockStatus.didJustFinish = false;
    mockStatus.currentTime = 0;
    mockStatus.duration = 0;
  });

  it('configures the native session for background playback', () => {
    renderPlayer();
    expect(setAudioModeAsync).toHaveBeenCalledWith(
      expect.objectContaining({
        playsInSilentMode: true,
        shouldPlayInBackground: true,
        interruptionMode: 'doNotMix',
      }),
    );
  });

  it('activates lock-screen metadata before play', async () => {
    const user = userEvent.setup();
    const order: string[] = [];
    mockPlayer.setActiveForLockScreen.mockImplementation(() => {
      order.push('lock');
    });
    mockPlayer.play.mockImplementation(() => {
      order.push('play');
    });
    renderPlayer();
    await user.press(screen.getByTestId('play-a'));

    expect(mockPlayer.replace).toHaveBeenCalledWith({
      uri: 'http://qz.test/api/v1/content/fan-performances/187/audio',
      headers: { Authorization: 'Bearer member-token' },
      name: trackA.title,
    });
    expect(mockPlayer.setActiveForLockScreen).toHaveBeenCalledWith(
      true,
      lockScreenMetadata(trackA),
      { ...lockScreenOptions },
    );
    expect(order).toEqual(['lock', 'play']);
    expect(screen.getByTestId('current')).toHaveTextContent(trackA.title);
    expect(JSON.stringify(mockPlayer.setActiveForLockScreen.mock.calls[0])).not.toContain('member-token');
    expect(lockScreenMetadata(trackA).albumTitle).toBe(lockScreenAlbumTitle);
  });

  it('refuses to load without a member token', async () => {
    mockSession.accessToken = null;
    const user = userEvent.setup();
    renderPlayer();
    await user.press(screen.getByTestId('play-a'));
    expect(screen.getByTestId('error')).toHaveTextContent('Sign in with a member account to stream recordings.');
    expect(mockPlayer.replace).not.toHaveBeenCalled();
    expect(mockPlayer.setActiveForLockScreen).not.toHaveBeenCalled();
  });

  it('advances lock-screen metadata to the next queued track', async () => {
    const user = userEvent.setup();
    const { rerender } = renderPlayer();
    await user.press(screen.getByTestId('play-a'));
    mockPlayer.setActiveForLockScreen.mockClear();
    mockStatus.didJustFinish = true;
    rerender(
      <FanPerformancePlayerProvider>
        <Probe />
      </FanPerformancePlayerProvider>,
    );
    expect(screen.getByTestId('current')).toHaveTextContent(trackB.title);
    expect(mockPlayer.setActiveForLockScreen).toHaveBeenCalledWith(
      true,
      lockScreenMetadata(trackB),
      { ...lockScreenOptions },
    );
    expect(mockPlayer.clearLockScreenControls).not.toHaveBeenCalled();
  });

  it('clears lock-screen controls when the queue ends', async () => {
    const user = userEvent.setup();
    const { rerender } = renderPlayer();
    await user.press(screen.getByTestId('play-last'));
    mockStatus.didJustFinish = true;
    rerender(
      <FanPerformancePlayerProvider>
        <Probe />
      </FanPerformancePlayerProvider>,
    );
    expect(mockPlayer.pause).toHaveBeenCalled();
    expect(mockPlayer.clearLockScreenControls).toHaveBeenCalled();
    expect(screen.getByTestId('current')).toHaveTextContent('none');
  });

  it('stops playback and clears controls on sign-out', async () => {
    const user = userEvent.setup();
    const { rerender } = renderPlayer();
    await user.press(screen.getByTestId('play-a'));
    mockSession.accessToken = null;
    rerender(
      <FanPerformancePlayerProvider>
        <Probe />
      </FanPerformancePlayerProvider>,
    );
    expect(mockPlayer.pause).toHaveBeenCalled();
    expect(mockPlayer.clearLockScreenControls).toHaveBeenCalled();
    expect(screen.getByTestId('current')).toHaveTextContent('none');
    expect(screen.getByTestId('queue')).toHaveTextContent('0');
  });

  it('pauses the same player from in-app toggle', async () => {
    const user = userEvent.setup();
    mockStatus.playing = true;
    const { rerender } = renderPlayer();
    await user.press(screen.getByTestId('play-a'));
    rerender(
      <FanPerformancePlayerProvider>
        <Probe />
      </FanPerformancePlayerProvider>,
    );
    await user.press(screen.getByTestId('toggle'));
    expect(mockPlayer.pause).toHaveBeenCalled();
  });

  it('resumes, seeks, skips, and steps the in-app queue', async () => {
    const user = userEvent.setup();
    mockStatus.playing = false;
    mockStatus.currentTime = 40;
    mockStatus.duration = 320;
    const { rerender } = renderPlayer();
    await user.press(screen.getByTestId('play-last'));
    rerender(
      <FanPerformancePlayerProvider>
        <Probe />
      </FanPerformancePlayerProvider>,
    );
    await user.press(screen.getByTestId('toggle'));
    await user.press(screen.getByTestId('seek'));
    await user.press(screen.getByTestId('skip'));
    await user.press(screen.getByTestId('previous'));

    expect(mockPlayer.play).toHaveBeenCalled();
    expect(mockPlayer.seekTo).toHaveBeenCalledWith(12);
    expect(mockPlayer.seekTo).toHaveBeenCalledWith(55);
    expect(screen.getByTestId('current')).toHaveTextContent(trackA.title);
  });

  it('clears lock-screen controls on unmount', async () => {
    const user = userEvent.setup();
    const view = renderPlayer();
    await user.press(screen.getByTestId('play-a'));
    mockPlayer.clearLockScreenControls.mockClear();
    view.unmount();
    expect(mockPlayer.clearLockScreenControls).toHaveBeenCalled();
  });

  it('throws outside the provider', () => {
    expect(() => renderWithProviders(<Probe />, { navigation: false })).toThrow(
      'useFanPerformancePlayer must be used inside FanPerformancePlayerProvider',
    );
  });
});
