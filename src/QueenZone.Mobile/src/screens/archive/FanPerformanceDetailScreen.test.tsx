import { screen, waitFor } from '@testing-library/react-native';
import { fetchFanPerformanceDetail, fetchFanPerformancesPage } from '../../api';
import { fanPerformanceFixture, pagedResponse } from '../../test/fixtures';
import { createMockSession } from '../../test/mockSession';
import { fakeNavigation, renderWithProviders } from '../../test/render';
import { FanPerformanceDetailScreen } from './FanPerformanceDetailScreen';

const mockSession = createMockSession();
const mockPlayer = {
  current: null,
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
    fetchFanPerformanceDetail: jest.fn(),
    fetchFanPerformancesPage: jest.fn(),
  };
});

jest.mock('../../session/SessionContext', () => ({
  useSession: () => mockSession,
}));

jest.mock('../../audio/FanPerformancePlayer', () => ({
  useFanPerformancePlayer: () => mockPlayer,
}));

const fetchDetail = fetchFanPerformanceDetail as jest.MockedFunction<typeof fetchFanPerformanceDetail>;
const fetchPage = fetchFanPerformancesPage as jest.MockedFunction<typeof fetchFanPerformancesPage>;
const track = fanPerformanceFixture();

function renderDetail() {
  return renderWithProviders(
    <FanPerformanceDetailScreen
      navigation={fakeNavigation() as never}
      route={{ key: 'detail', name: 'FanPerformanceDetail', params: { id: track.id } } as never}
    />,
    { navigation: false },
  );
}

describe('FanPerformanceDetailScreen', () => {
  beforeEach(() => {
    mockSession.accessToken = null;
    mockSession.isRestoring = false;
    fetchDetail.mockResolvedValue(track);
    fetchPage.mockResolvedValue(pagedResponse([track]));
  });

  it('does not ask for sign-in while a previous session is still restoring', async () => {
    mockSession.isRestoring = true;
    renderDetail();
    await waitFor(() => expect(screen.getByText(track.title)).toBeOnTheScreen());
    expect(screen.getByTestId('fan-performance-session-restoring')).toBeOnTheScreen();
    expect(screen.queryByRole('button', { name: 'Sign in' })).toBeNull();
  });

  it('shows the sign-in prompt when restore finished without a member', async () => {
    renderDetail();
    await waitFor(() => expect(screen.getByText(track.title)).toBeOnTheScreen());
    expect(screen.getByRole('button', { name: 'Sign in' })).toBeOnTheScreen();
  });

  it('shows playback controls when a member token is present', async () => {
    mockSession.accessToken = 'member-token';
    renderDetail();
    await waitFor(() => expect(screen.getByLabelText('Play')).toBeOnTheScreen());
    expect(screen.queryByRole('button', { name: 'Sign in' })).toBeNull();
  });
});
