import { screen, userEvent, waitFor } from '@testing-library/react-native';
import { createMemoryStorage } from '../cache/storage';
import { resetExternalStoreForTests } from '../cache/externalStore';
import { fanPerformanceFixture, memberProfileFixture } from '../test/fixtures';
import { createMockSession } from '../test/mockSession';
import { renderWithProviders } from '../test/render';
import { testIds } from '../test/testIds';
import { FanPerformanceDownloadsList } from './FanPerformanceDownloadsList';
import { setDownloadManifestStorageForTests } from './manifest';
import { resetDownloadManagerForTests } from './manager';
import { resetDownloadUiForTests, setDownloadUiSnapshot, snapshotFromEntry } from './uiState';
import { createMemoryDownloadHost, setDownloadFileHostForTests } from './files';

const mockSession = createMockSession();
const mockPlayer = {
  current: null as { id: number } | null,
  playing: false,
  play: jest.fn(),
  toggle: jest.fn(),
};
const track = fanPerformanceFixture();

jest.mock('../session/SessionContext', () => ({
  useSession: () => mockSession,
}));

jest.mock('../audio/FanPerformancePlayer', () => ({
  useFanPerformancePlayer: () => mockPlayer,
}));

describe('FanPerformanceDownloadsList', () => {
  beforeEach(() => {
    resetDownloadManagerForTests();
    resetDownloadUiForTests();
    resetExternalStoreForTests();
    setDownloadManifestStorageForTests(createMemoryStorage());
    setDownloadFileHostForTests(createMemoryDownloadHost());
    mockSession.profile = memberProfileFixture({ memberId: 'member-1' });
    mockSession.accessToken = 'member-token';
    mockPlayer.current = null;
    mockPlayer.playing = false;
    mockPlayer.play.mockReset();
    mockPlayer.toggle.mockReset();
    setDownloadUiSnapshot(
      'member-1',
      snapshotFromEntry({
        performanceId: String(track.id),
        localUri: 'file:///documents/fan-performances/187',
        title: track.title,
        performedBy: track.performedBy,
        byteSize: 4096,
        sourceRevision: '"etag-1"',
        completedAt: '2026-09-05T00:00:00.000Z',
        memberId: 'member-1',
      }),
    );
  });

  it('plays and removes a downloaded recording', async () => {
    renderWithProviders(<FanPerformanceDownloadsList />, { navigation: false });
    expect(screen.getByText(track.title)).toBeOnTheScreen();
    expect(screen.getByText(/4.0 KB/)).toBeOnTheScreen();

    const user = userEvent.setup();
    await user.press(screen.getByTestId(`${testIds.fanPerformanceDownloadPlayPrefix}${track.id}`));
    expect(mockPlayer.play).toHaveBeenCalled();

    await user.press(screen.getByTestId(`${testIds.fanPerformanceDownloadRemovePrefix}${track.id}`));
    await waitFor(() => expect(screen.getByText('No downloaded recordings yet.')).toBeOnTheScreen());
  });
});
