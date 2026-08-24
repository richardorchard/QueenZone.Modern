import { screen, userEvent, waitFor } from '@testing-library/react-native';
import { fetchInbox } from '../../api/messages';
import { ApiError } from '../../api/client';
import { pagedResponse } from '../../test/fixtures';
import { createMockSession } from '../../test/mockSession';
import { fakeNavigation, renderWithProviders } from '../../test/render';
import { InboxScreen } from './InboxScreen';

const mockSession = createMockSession();

jest.mock('../../session/SessionContext', () => ({
  useSession: () => mockSession,
}));

jest.mock('../../api/messages', () => ({
  fetchInbox: jest.fn(),
}));

jest.mock('@react-navigation/native', () => {
  const actual = jest.requireActual('@react-navigation/native');
  return {
    ...actual,
    useNavigation: () => ({ navigate: jest.fn() }),
  };
});

const fetchInboxMock = fetchInbox as jest.MockedFunction<typeof fetchInbox>;

function renderInbox() {
  return renderWithProviders(
    <InboxScreen navigation={fakeNavigation() as never} route={{ key: 'inbox', name: 'Inbox' } as never} />,
  );
}

describe('InboxScreen', () => {
  beforeEach(() => {
    mockSession.isSignedIn = false;
    mockSession.accessToken = null;
    fetchInboxMock.mockReset();
  });

  it('gates unsigned visitors', () => {
    renderInbox();
    expect(screen.getByText('Messages')).toBeOnTheScreen();
    expect(screen.getByRole('button', { name: 'Sign in' })).toBeOnTheScreen();
  });

  it('shows empty, error, and success list states', async () => {
    mockSession.isSignedIn = true;
    mockSession.accessToken = 'tok';
    fetchInboxMock.mockResolvedValueOnce(pagedResponse([], 1, 0));
    renderInbox();
    await waitFor(() => expect(screen.getByText('You have no private messages yet.')).toBeOnTheScreen());
  });

  it('opens a conversation from a labelled inbox row', async () => {
    const navigation = fakeNavigation();
    mockSession.isSignedIn = true;
    mockSession.accessToken = 'tok';
    fetchInboxMock.mockResolvedValueOnce(
      pagedResponse(
        [
          {
            conversationId: 'convo-1',
            otherParticipantId: 'member-2',
            otherParticipantDisplayName: 'Brian',
            lastMessagePreview: 'See you at Wembley',
            lastMessageAt: '2024-01-15T12:00:00.000Z',
            hasUnread: true,
            unreadCount: 2,
            detailPath: '/messages/convo-1',
          },
        ],
        1,
        1,
      ),
    );
    renderWithProviders(
      <InboxScreen navigation={navigation as never} route={{ key: 'inbox', name: 'Inbox' } as never} />,
    );
    await waitFor(() => expect(screen.getByText('Brian')).toBeOnTheScreen());
    const user = userEvent.setup();
    await user.press(screen.getByRole('button', { name: /Brian/ }));
    expect(navigation.navigate).toHaveBeenCalledWith('Conversation', { id: 'convo-1' });
  });

  it('shows a retryable error', async () => {
    mockSession.isSignedIn = true;
    mockSession.accessToken = 'tok';
    fetchInboxMock.mockRejectedValueOnce(new ApiError(500, 'The server had a problem. Try again shortly.'));
    renderInbox();
    await waitFor(() => expect(screen.getByText('Unable to load')).toBeOnTheScreen());
    expect(screen.getByRole('button', { name: 'Try again' })).toBeOnTheScreen();
  });
});
