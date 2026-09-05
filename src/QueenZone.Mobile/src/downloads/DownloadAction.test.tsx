import { screen, userEvent, waitFor } from '@testing-library/react-native';
import { createMemoryStorage } from '../cache/storage';
import { resetExternalStoreForTests } from '../cache/externalStore';
import { fanPerformanceFixture, memberProfileFixture } from '../test/fixtures';
import { createMockSession } from '../test/mockSession';
import { renderWithProviders } from '../test/render';
import { testIds } from '../test/testIds';
import { createMemoryDownloadHost, setDownloadFileHostForTests } from './files';
import { setDownloadManifestStorageForTests } from './manifest';
import { resetDownloadManagerForTests, setDownloadProbeForTests } from './manager';
import { resetDownloadUiForTests, setDownloadUiSnapshot, transientSnapshot } from './uiState';
import { DownloadAction } from './DownloadAction';

const mockSession = createMockSession();
const track = fanPerformanceFixture();

jest.mock('../session/SessionContext', () => ({
  useSession: () => mockSession,
}));

function reset() {
  resetDownloadManagerForTests();
  resetDownloadUiForTests();
  resetExternalStoreForTests();
  setDownloadManifestStorageForTests(createMemoryStorage());
  setDownloadFileHostForTests(createMemoryDownloadHost());
  setDownloadProbeForTests(async () => ({ status: 206, sourceRevision: '"e"', byteSize: 4 }));
  mockSession.accessToken = 'member-token';
  mockSession.isSignedIn = true;
  mockSession.profile = memberProfileFixture({ memberId: 'member-1' });
  mockSession.ensureAccessToken.mockResolvedValue('member-token');
}

describe('DownloadAction', () => {
  beforeEach(reset);
  afterEach(() => {
    setDownloadFileHostForTests(null);
    setDownloadManifestStorageForTests(null);
  });

  it('starts a download and ignores a second tap while queued', async () => {
    let release!: () => void;
    const held = new Promise<void>((resolve) => {
      release = resolve;
    });
    const host = createMemoryDownloadHost({
      downloadImpl: async ({ destUri }) => {
        await held;
        host.files.set(destUri, new Uint8Array([1, 2, 3, 4]));
      },
    });
    setDownloadFileHostForTests(host);

    renderWithProviders(<DownloadAction track={track} />, { navigation: false });
    const user = userEvent.setup();
    const button = () => screen.getByTestId(`${testIds.fanPerformanceDownloadPrefix}${track.id}`);
    await user.press(button());
    await waitFor(() =>
      expect(screen.getByLabelText(new RegExp(`(queued|Downloading) ${track.title}`))).toBeOnTheScreen(),
    );
    await user.press(button());
    expect(screen.getByLabelText(new RegExp(`(queued|Downloading) ${track.title}`))).toBeOnTheScreen();

    release();
    await waitFor(() => expect(screen.getByLabelText(`${track.title} downloaded, 4 B`)).toBeOnTheScreen());
  });

  it('announces a failed download and retries', async () => {
    setDownloadUiSnapshot(
      'member-1',
      transientSnapshot(String(track.id), 'failed', {
        title: track.title,
        performedBy: track.performedBy,
        error: 'Could not download this recording. Try again.',
      }),
    );
    renderWithProviders(<DownloadAction track={track} />, { navigation: false });
    expect(screen.getByLabelText(`Download failed for ${track.title}. Double tap to retry`)).toBeOnTheScreen();
  });
});
