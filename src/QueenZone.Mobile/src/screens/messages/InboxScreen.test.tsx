import { act, screen, userEvent, waitFor } from '@testing-library/react-native';
import { archiveConversation, fetchInbox } from '../../api/messages';
import { ApiError } from '../../api/client';
import { getContentCache } from '../../cache';
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
  archiveConversation: jest.fn(),
}));

jest.mock('../../cache', () => ({
  ...jest.requireActual('../../cache'),
  getContentCache: jest.fn(),
}));

jest.mock('@react-navigation/native', () => {
  const actual = jest.requireActual('@react-navigation/native');
  return {
    ...actual,
    useNavigation: () => ({ navigate: jest.fn() }),
  };
});

const fetchInboxMock = fetchInbox as jest.MockedFunction<typeof fetchInbox>;
const archiveConversationMock = archiveConversation as jest.MockedFunction<typeof archiveConversation>;
const getContentCacheMock = getContentCache as jest.MockedFunction<typeof getContentCache>;

function fakeContentCache() {
  return {
    get: jest.fn().mockResolvedValue(null),
    put: jest.fn().mockResolvedValue(undefined),
  };
}

function renderInbox() {
  return renderWithProviders(
    <InboxScreen navigation={fakeNavigation() as never} route={{ key: 'inbox', name: 'Inbox' } as never} />,
  );
}

describe('InboxScreen', () => {
  beforeEach(() => {
    mockSession.isSignedIn = false;
    mockSession.accessToken = null;
    mockSession.profile = null;
    fetchInboxMock.mockReset();
    archiveConversationMock.mockReset();
    getContentCacheMock.mockReset();
    getContentCacheMock.mockReturnValue(
      fakeContentCache() as unknown as ReturnType<typeof getContentCache>,
    );
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

  it('navigates to the archived list', async () => {
    const navigation = fakeNavigation();
    mockSession.isSignedIn = true;
    mockSession.accessToken = 'tok';
    fetchInboxMock.mockResolvedValueOnce(pagedResponse([], 1, 0));
    renderWithProviders(
      <InboxScreen navigation={navigation as never} route={{ key: 'inbox', name: 'Inbox' } as never} />,
    );
    await waitFor(() => expect(screen.getByText('You have no private messages yet.')).toBeOnTheScreen());
    const user = userEvent.setup();
    await user.press(screen.getByRole('button', { name: 'Archived' }));
    expect(navigation.navigate).toHaveBeenCalledWith('Archived');
  });

  it('archives a conversation from the inbox row and refreshes the list', async () => {
    mockSession.isSignedIn = true;
    mockSession.accessToken = 'tok';
    fetchInboxMock.mockResolvedValue(
      pagedResponse(
        [
          {
            conversationId: 'convo-1',
            otherParticipantId: 'member-2',
            otherParticipantDisplayName: 'Brian',
            lastMessagePreview: 'See you at Wembley',
            lastMessageAt: '2024-01-15T12:00:00.000Z',
            hasUnread: false,
            unreadCount: 0,
            detailPath: '/messages/convo-1',
          },
        ],
        1,
        1,
      ),
    );
    archiveConversationMock.mockResolvedValueOnce(undefined);
    renderInbox();
    await waitFor(() => expect(screen.getByText('Brian')).toBeOnTheScreen());

    const user = userEvent.setup();
    await user.press(screen.getByRole('button', { name: 'Archive' }));

    await waitFor(() => expect(archiveConversationMock).toHaveBeenCalledWith('tok', 'convo-1'));
    await waitFor(() => expect(fetchInboxMock).toHaveBeenCalledTimes(2));
  });

  it('shows markup and URLs in the preview as plain text', async () => {
    mockSession.isSignedIn = true;
    mockSession.accessToken = 'tok';
    fetchInboxMock.mockResolvedValueOnce(
      pagedResponse(
        [
          {
            conversationId: 'convo-2',
            otherParticipantId: 'member-3',
            otherParticipantDisplayName: 'John',
            lastMessagePreview: '<script>alert(1)</script> https://example.com',
            lastMessageAt: '2024-01-15T12:00:00.000Z',
            hasUnread: false,
            unreadCount: 0,
            detailPath: '/messages/convo-2',
          },
        ],
        1,
        1,
      ),
    );
    renderInbox();
    await waitFor(() =>
      expect(screen.getByText('<script>alert(1)</script> https://example.com')).toBeOnTheScreen(),
    );
    expect(screen.queryByRole('link')).toBeNull();
  });

  it('shows a retryable error', async () => {
    mockSession.isSignedIn = true;
    mockSession.accessToken = 'tok';
    fetchInboxMock.mockRejectedValueOnce(new ApiError(500, 'The server had a problem. Try again shortly.'));
    renderInbox();
    await waitFor(() => expect(screen.getByText('Unable to load')).toBeOnTheScreen());
    expect(screen.getByRole('button', { name: 'Try again' })).toBeOnTheScreen();
  });

  it('renders a cached inbox instantly while the fresh fetch is still in flight', async () => {
    mockSession.isSignedIn = true;
    mockSession.accessToken = 'tok';
    mockSession.profile = { memberId: 'member-1' } as never;
    const cache = fakeContentCache();
    cache.get.mockResolvedValue([
      {
        conversationId: 'convo-cached',
        otherParticipantId: 'member-9',
        otherParticipantDisplayName: 'Cached Carol',
        lastMessagePreview: 'From last visit',
        lastMessageAt: '2024-01-15T12:00:00.000Z',
        hasUnread: false,
        unreadCount: 0,
        detailPath: '/messages/convo-cached',
      },
    ]);
    getContentCacheMock.mockReturnValue(cache as unknown as ReturnType<typeof getContentCache>);
    let resolveFetch: (value: Awaited<ReturnType<typeof fetchInbox>>) => void = () => {};
    fetchInboxMock.mockReturnValueOnce(
      new Promise((resolve) => {
        resolveFetch = resolve;
      }),
    );
    renderInbox();

    expect(cache.get).toHaveBeenCalledWith('messages:member:member-1:inbox');
    await waitFor(() => expect(screen.getByText('Cached Carol')).toBeOnTheScreen());

    await act(async () => {
      resolveFetch(pagedResponse([], 1, 0));
    });
    await waitFor(() => expect(screen.getByText('You have no private messages yet.')).toBeOnTheScreen());
  });

  it('persists the freshly loaded first page to the cache', async () => {
    mockSession.isSignedIn = true;
    mockSession.accessToken = 'tok';
    mockSession.profile = { memberId: 'member-1' } as never;
    const cache = fakeContentCache();
    getContentCacheMock.mockReturnValue(cache as unknown as ReturnType<typeof getContentCache>);
    fetchInboxMock.mockResolvedValueOnce(
      pagedResponse(
        [
          {
            conversationId: 'convo-1',
            otherParticipantId: 'member-2',
            otherParticipantDisplayName: 'Brian',
            lastMessagePreview: 'See you at Wembley',
            lastMessageAt: '2024-01-15T12:00:00.000Z',
            hasUnread: false,
            unreadCount: 0,
            detailPath: '/messages/convo-1',
          },
        ],
        1,
        1,
      ),
    );
    renderInbox();
    await waitFor(() => expect(screen.getByText('Brian')).toBeOnTheScreen());
    await waitFor(() =>
      expect(cache.put).toHaveBeenCalledWith(
        'messages:member:member-1:inbox',
        expect.arrayContaining([expect.objectContaining({ conversationId: 'convo-1' })]),
      ),
    );
  });
});
