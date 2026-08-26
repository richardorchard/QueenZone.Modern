import { fireEvent, screen, waitFor } from '@testing-library/react-native';
import * as ImagePicker from 'expo-image-picker';
import { fetchJson, sendJson } from '../../api/client';
import { ApiError } from '../../api/errors';
import { appendUploadFile } from '../../api/uploadFile';
import { reportApiFailure } from '../../config/sentry';
import { createMockSession } from '../../test/mockSession';
import { fakeNavigation, renderWithProviders } from '../../test/render';
import { testIds } from '../../test/testIds';
import { SettingsScreen } from './SettingsScreen';

const mockSession = createMockSession();

jest.mock('../../session/SessionContext', () => ({
  useSession: () => mockSession,
}));

jest.mock('@react-navigation/native', () => {
  const actual = jest.requireActual('@react-navigation/native');
  return {
    ...actual,
    useNavigation: () => ({ navigate: jest.fn(), dispatch: jest.fn() }),
  };
});

jest.mock('../../config/appConfig', () => ({
  getAppConfig: () => ({ apiBaseUrl: 'http://qz.test', appEnv: 'development', version: '0.1.0' }),
}));

jest.mock('../../api/client', () => ({
  fetchJson: jest.fn(),
  sendJson: jest.fn(),
  sendMultipart: jest.fn(),
}));

jest.mock('../../api/uploadFile', () => ({
  appendUploadFile: jest.fn(),
}));

jest.mock('../../config/sentry', () => ({
  reportApiFailure: jest.fn(),
}));

jest.mock('expo-image-picker', () => ({
  requestMediaLibraryPermissionsAsync: jest.fn(),
  requestCameraPermissionsAsync: jest.fn(),
  launchImageLibraryAsync: jest.fn(),
  launchCameraAsync: jest.fn(),
}));

const fetchJsonMock = fetchJson as jest.MockedFunction<typeof fetchJson>;
const sendJsonMock = sendJson as jest.MockedFunction<typeof sendJson>;
const appendUploadFileMock = appendUploadFile as jest.MockedFunction<typeof appendUploadFile>;
const reportApiFailureMock = reportApiFailure as jest.MockedFunction<typeof reportApiFailure>;

const profilePayload = {
  memberId: '11111111-1111-1111-1111-111111111111',
  email: 'fan@example.com',
  displayName: 'Roger',
};

const defaultPreferences = { forumReply: true, privateMessage: true, news: false };

function mockSettingsLoad(preferences = defaultPreferences) {
  fetchJsonMock.mockImplementation(async (path: string) => {
    if (path === '/me') {
      return profilePayload;
    }
    if (path === '/me/notification-preferences') {
      return preferences;
    }
    throw new Error(`unexpected GET ${path}`);
  });
}

function renderSettings() {
  return renderWithProviders(
    <SettingsScreen navigation={fakeNavigation() as never} route={{ key: 'settings', name: 'Settings' } as never} />,
  );
}

describe('SettingsScreen notifications', () => {
  beforeEach(() => {
    mockSession.isSignedIn = true;
    mockSession.isRestoring = false;
    mockSession.accessToken = 'tok';
    mockSession.refreshProfile.mockResolvedValue(undefined);
    mockSettingsLoad();
    sendJsonMock.mockReset();
    appendUploadFileMock.mockReset();
    reportApiFailureMock.mockReset();
  });

  it('gates unsigned visitors', () => {
    mockSession.isSignedIn = false;
    mockSession.accessToken = null;
    renderSettings();
    expect(screen.getByText('Settings')).toBeOnTheScreen();
    expect(screen.getByRole('button', { name: 'Sign in' })).toBeOnTheScreen();
  });

  it('loads independent toggles from the preferences endpoint', async () => {
    mockSettingsLoad({ forumReply: true, privateMessage: false, news: false });
    renderSettings();

    await waitFor(() => expect(screen.getByTestId(testIds.settingsNotifyNews)).toBeOnTheScreen());
    expect(screen.getByText('Notifications')).toBeOnTheScreen();
    expect(screen.getByRole('switch', { name: 'Forum replies' })).toBeOnTheScreen();
    expect(screen.getByRole('switch', { name: 'Private messages' })).toHaveProp('value', false);
    expect(screen.getByRole('switch', { name: 'News' })).toHaveProp('value', false);
    expect(screen.getByText('You still need to Watch a topic to get forum reply pushes.')).toBeOnTheScreen();
    expect(fetchJsonMock).toHaveBeenCalledWith('/me/notification-preferences', { accessToken: 'tok' });
  });

  it('persists a toggle immediately and reports status', async () => {
    sendJsonMock.mockResolvedValue({ forumReply: true, privateMessage: true, news: true });
    renderSettings();
    await waitFor(() => expect(screen.getByRole('switch', { name: 'News' })).toBeOnTheScreen());

    fireEvent(screen.getByRole('switch', { name: 'News' }), 'valueChange', true);

    await waitFor(() =>
      expect(sendJsonMock).toHaveBeenCalledWith('/me/notification-preferences', {
        method: 'PATCH',
        accessToken: 'tok',
        body: { news: true },
      }),
    );
    expect(screen.getByText('Notification preferences updated.')).toBeOnTheScreen();
    expect(screen.getByRole('switch', { name: 'News' })).toHaveProp('value', true);
  });

  it('reverts the toggle when save fails', async () => {
    sendJsonMock.mockRejectedValue(new ApiError(500, 'The server had a problem. Try again shortly.'));
    renderSettings();
    await waitFor(() => expect(screen.getByRole('switch', { name: 'News' })).toHaveProp('value', false));

    fireEvent(screen.getByRole('switch', { name: 'News' }), 'valueChange', true);

    await waitFor(() =>
      expect(screen.getByText('The server had a problem. Try again shortly.')).toBeOnTheScreen(),
    );
    expect(screen.getByRole('switch', { name: 'News' })).toHaveProp('value', false);
  });

  it('reports a local-file avatar failure instead of a fake offline message', async () => {
    const cause = new TypeError('Network request failed');
    appendUploadFileMock.mockRejectedValueOnce(ApiError.localFile(cause));
    (ImagePicker.requestMediaLibraryPermissionsAsync as jest.Mock).mockResolvedValue({ granted: true });
    (ImagePicker.launchImageLibraryAsync as jest.Mock).mockResolvedValue({
      canceled: false,
      assets: [{ uri: 'file:///tmp/avatar.jpg', fileName: 'avatar.jpg', mimeType: 'image/jpeg' }],
    });

    renderSettings();
    await waitFor(() => expect(screen.getByRole('button', { name: 'Choose photo' })).toBeOnTheScreen());
    fireEvent.press(screen.getByRole('button', { name: 'Choose photo' }));

    await waitFor(() =>
      expect(screen.getByText('Could not read the selected photo. Try choosing it again.')).toBeOnTheScreen(),
    );
    expect(reportApiFailureMock).toHaveBeenCalledWith({
      kind: 'local-file',
      status: 0,
      method: 'POST',
      path: '/me/avatar',
      cause,
    });
    expect(screen.queryByText('Unable to reach QueenZone. Check your connection and try again.')).toBeNull();
  });
});
