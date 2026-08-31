import { screen, userEvent, waitFor } from '@testing-library/react-native';
import { fetchJson, sendJson } from '../../api/client';
import { ApiError } from '../../api/errors';
import { memberProfilePayload } from '../../test/fixtures';
import { createMockSession } from '../../test/mockSession';
import { renderWithProviders } from '../../test/render';
import { DeleteAccountScreen } from './DeleteAccountScreen';

const mockSession = createMockSession();

jest.mock('../../session/SessionContext', () => ({
  useSession: () => mockSession,
}));

jest.mock('../../api/client', () => ({
  fetchJson: jest.fn(),
  sendJson: jest.fn(),
}));

const fetchJsonMock = fetchJson as jest.MockedFunction<typeof fetchJson>;
const sendJsonMock = sendJson as jest.MockedFunction<typeof sendJson>;

const deletionProfile = memberProfilePayload({
  deletion: {
    confirmationPhrase: 'DELETE',
    confirmationHint: 'Type DELETE to schedule deletion of the account.',
    requestedTitle: 'Account deletion scheduled',
    requestedMessage: 'You have been signed out.',
    whatHappens: ['Your public posts stay in the archive.'],
  },
});

function renderDeleteAccount() {
  return renderWithProviders(<DeleteAccountScreen />);
}

describe('DeleteAccountScreen', () => {
  beforeEach(() => {
    mockSession.isSignedIn = true;
    mockSession.accessToken = 'tok';
    mockSession.signOut.mockReset();
    mockSession.refreshProfile.mockReset();
    mockSession.signOut.mockResolvedValue(undefined);
    mockSession.refreshProfile.mockResolvedValue(undefined);
    fetchJsonMock.mockReset();
    sendJsonMock.mockReset();
    fetchJsonMock.mockResolvedValue(deletionProfile);
  });

  it('does not POST when the confirmation phrase is wrong', async () => {
    renderDeleteAccount();
    await waitFor(() => expect(screen.getByLabelText('Type DELETE to confirm')).toBeOnTheScreen());

    const user = userEvent.setup();
    await user.type(screen.getByLabelText('Type DELETE to confirm'), 'NOPE');
    await user.press(screen.getByRole('button', { name: 'Schedule account deletion' }));

    await waitFor(() =>
      expect(screen.getByText('Type DELETE to schedule deletion of the account.')).toBeOnTheScreen(),
    );
    expect(sendJsonMock).not.toHaveBeenCalled();
    expect(mockSession.signOut).not.toHaveBeenCalled();
  });

  it('schedules deletion and signs out', async () => {
    sendJsonMock.mockResolvedValueOnce({
      requested: true,
      scheduledDeletionAt: '2026-09-26T00:00:00.000Z',
      title: 'Account deletion scheduled',
      message: 'You have been signed out.',
    });
    renderDeleteAccount();
    await waitFor(() => expect(screen.getByLabelText('Type DELETE to confirm')).toBeOnTheScreen());

    const user = userEvent.setup();
    await user.type(screen.getByLabelText('Type DELETE to confirm'), 'DELETE');
    await user.press(screen.getByRole('button', { name: 'Schedule account deletion' }));

    await waitFor(() => expect(screen.getByText('Account deletion scheduled')).toBeOnTheScreen());
    expect(sendJsonMock).toHaveBeenCalledWith('/me/deletion-request', {
      accessToken: 'tok',
      body: { confirmation: 'DELETE' },
    });
    expect(mockSession.signOut).toHaveBeenCalled();
    expect(screen.getByText('You have been signed out.')).toBeOnTheScreen();
  });

  it('cancels a scheduled deletion and refreshes the profile', async () => {
    fetchJsonMock.mockResolvedValueOnce(
      memberProfilePayload({
        scheduledDeletionAt: '2026-09-26T00:00:00.000Z',
      }),
    );
    sendJsonMock.mockResolvedValueOnce(deletionProfile);
    renderDeleteAccount();
    await waitFor(() => expect(screen.getByRole('button', { name: 'Cancel account deletion' })).toBeOnTheScreen());

    const user = userEvent.setup();
    await user.press(screen.getByRole('button', { name: 'Cancel account deletion' }));

    await waitFor(() =>
      expect(sendJsonMock).toHaveBeenCalledWith('/me/deletion-request/cancel', { accessToken: 'tok' }),
    );
    expect(mockSession.refreshProfile).toHaveBeenCalled();
    await waitFor(() => expect(screen.getByRole('button', { name: 'Schedule account deletion' })).toBeOnTheScreen());
  });

  it('shows an API error when schedule fails', async () => {
    sendJsonMock.mockRejectedValueOnce(new ApiError(500, 'The server had a problem.'));
    renderDeleteAccount();
    await waitFor(() => expect(screen.getByLabelText('Type DELETE to confirm')).toBeOnTheScreen());

    const user = userEvent.setup();
    await user.type(screen.getByLabelText('Type DELETE to confirm'), 'DELETE');
    await user.press(screen.getByRole('button', { name: 'Schedule account deletion' }));

    await waitFor(() => expect(screen.getByText('The server had a problem.')).toBeOnTheScreen());
    expect(mockSession.signOut).not.toHaveBeenCalled();
  });
});
