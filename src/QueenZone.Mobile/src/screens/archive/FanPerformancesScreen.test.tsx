import { screen, userEvent, waitFor } from '@testing-library/react-native';
import { fetchFanPerformancesPage } from '../../api';
import { fanPerformanceFixture, pagedResponse } from '../../test/fixtures';
import { createMockSession } from '../../test/mockSession';
import { fakeNavigation, flushVirtualizedList, renderWithProviders } from '../../test/render';
import { testIds } from '../../test/testIds';
import { FanPerformancesScreen } from './FanPerformancesScreen';

const mockSession = createMockSession();
const mockPlayer = {
  current: null as { id: number } | null,
  queue: [],
  error: null,
  playing: false,
  currentTime: 0,
  duration: 0,
  isLoaded: false,
  isBuffering: false,
  play: jest.fn(),
  toggle: jest.fn(),
  seekTo: jest.fn(),
  skip: jest.fn(),
  playNext: jest.fn(),
  playPrevious: jest.fn(),
};

jest.mock('../../api', () => {
  const actual = jest.requireActual('../../api');
  return {
    ...actual,
    fetchFanPerformancesPage: jest.fn(),
  };
});

jest.mock('../../session/SessionContext', () => ({
  useSession: () => mockSession,
}));

jest.mock('../../audio/FanPerformancePlayer', () => ({
  useFanPerformancePlayer: () => mockPlayer,
}));

const fetchPage = fetchFanPerformancesPage as jest.MockedFunction<typeof fetchFanPerformancesPage>;
const track = fanPerformanceFixture();

function renderList() {
  const navigation = fakeNavigation();
  return {
    navigation,
    ...renderWithProviders(
      <FanPerformancesScreen
        navigation={navigation as never}
        route={{ key: 'list', name: 'FanPerformances' } as never}
      />,
      { navigation: false },
    ),
  };
}

describe('FanPerformancesScreen', () => {
  beforeEach(() => {
    mockSession.accessToken = null;
    mockSession.isRestoring = false;
    mockPlayer.current = null;
    mockPlayer.playing = false;
    mockPlayer.play.mockReset();
    mockPlayer.toggle.mockReset();
    fetchPage.mockResolvedValue(pagedResponse([track]));
  });

  afterEach(async () => {
    await flushVirtualizedList();
  });

  it('opens detail from the title and sends unsigned visitors to sign in from play', async () => {
    const { navigation } = renderList();
    await waitFor(() => expect(screen.getByText(track.title)).toBeOnTheScreen());

    expect(screen.getByText('Sign in to play')).toBeOnTheScreen();
    const user = userEvent.setup();
    await user.press(screen.getByTestId(`${testIds.fanPerformanceRowPrefix}${track.id}`));
    expect(navigation.navigate).toHaveBeenCalledWith('FanPerformanceDetail', { id: track.id });
    expect(mockPlayer.play).not.toHaveBeenCalled();

    await user.press(screen.getByRole('button', { name: `Sign in to play ${track.title}` }));
    expect(navigation.dispatch).toHaveBeenCalledWith(
      expect.objectContaining({
        type: 'NAVIGATE',
        payload: expect.objectContaining({
          name: 'SignIn',
          params: { returnTo: { tab: 'ArchiveTab', screen: 'FanPerformances' } },
        }),
      }),
    );
    expect(navigation.navigate).toHaveBeenCalledTimes(1);
  });

  it('does not open sign-in from play while a session is restoring', async () => {
    mockSession.isRestoring = true;
    const { navigation } = renderList();
    await waitFor(() => expect(screen.getByText(track.title)).toBeOnTheScreen());

    const user = userEvent.setup();
    await user.press(screen.getByTestId(`${testIds.fanPerformancePlayPrefix}${track.id}`));
    expect(navigation.dispatch).not.toHaveBeenCalled();
    expect(mockPlayer.play).not.toHaveBeenCalled();
  });

  it('plays from the list when a member is signed in and still opens detail from the title', async () => {
    mockSession.accessToken = 'member-token';
    const { navigation } = renderList();
    await waitFor(() => expect(screen.getByRole('button', { name: `Play ${track.title}` })).toBeOnTheScreen());

    const user = userEvent.setup();
    await user.press(screen.getByRole('button', { name: `Play ${track.title}` }));
    await waitFor(() => expect(mockPlayer.play).toHaveBeenCalledWith(track, [track]));
    expect(navigation.navigate).not.toHaveBeenCalled();

    await user.press(screen.getByRole('button', { name: `Open ${track.title}` }));
    expect(navigation.navigate).toHaveBeenCalledWith('FanPerformanceDetail', { id: track.id });
  });

  it('toggles pause when the listed track is already playing', async () => {
    mockSession.accessToken = 'member-token';
    mockPlayer.current = { id: track.id };
    mockPlayer.playing = true;
    renderList();
    await waitFor(() => expect(screen.getByRole('button', { name: `Pause ${track.title}` })).toBeOnTheScreen());

    const user = userEvent.setup();
    await user.press(screen.getByRole('button', { name: `Pause ${track.title}` }));
    expect(mockPlayer.toggle).toHaveBeenCalledTimes(1);
    expect(mockPlayer.play).not.toHaveBeenCalled();
  });
});
