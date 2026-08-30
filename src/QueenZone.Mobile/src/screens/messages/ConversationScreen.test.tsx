import { Alert } from 'react-native';
import { screen, userEvent, waitFor } from '@testing-library/react-native';
import { ApiError } from '../../api/client';
import {
  archiveConversation,
  blockConversationParticipant,
  fetchConversation,
  replyToConversation,
  reportConversationMessage,
} from '../../api/messages';
import { getMessagesCache } from '../../cache/messagesCache';
import { createMockSession } from '../../test/mockSession';
import { fakeNavigation, flushVirtualizedList, renderWithProviders } from '../../test/render';
import { ConversationScreen } from './ConversationScreen';

const conversationId = 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee';
const theirMessageId = '11111111-2222-3333-4444-555555555555';
const mockSession = createMockSession();

jest.mock('../../session/SessionContext', () => ({
  useSession: () => mockSession,
}));

jest.mock('../../api/messages', () => ({
  fetchConversation: jest.fn(),
  replyToConversation: jest.fn(),
  reportConversationMessage: jest.fn(),
  archiveConversation: jest.fn(),
  blockConversationParticipant: jest.fn(),
}));

jest.mock('../../cache/messagesCache', () => ({
  getMessagesCache: jest.fn(),
}));

const fetchConversationMock = fetchConversation as jest.MockedFunction<typeof fetchConversation>;
const replyToConversationMock = replyToConversation as jest.MockedFunction<typeof replyToConversation>;
const reportConversationMessageMock = reportConversationMessage as jest.MockedFunction<
  typeof reportConversationMessage
>;
const archiveConversationMock = archiveConversation as jest.MockedFunction<typeof archiveConversation>;
const blockConversationParticipantMock = blockConversationParticipant as jest.MockedFunction<
  typeof blockConversationParticipant
>;
const getMessagesCacheMock = getMessagesCache as jest.MockedFunction<typeof getMessagesCache>;

function fakeMessagesCache() {
  return {
    get: jest.fn().mockResolvedValue(null),
    put: jest.fn().mockResolvedValue(undefined),
  };
}

function renderConversation() {
  return renderWithProviders(
    <ConversationScreen
      navigation={fakeNavigation() as never}
      route={{ key: 'conversation', name: 'Conversation', params: { id: conversationId } } as never}
    />,
  );
}

function conversationDetail(
  messages: Array<{
    id: string;
    senderMemberId: string;
    senderDisplayName: string;
    body: string;
    createdAt: string;
    isMine: boolean;
    sortKey: number;
    reportedByViewer?: boolean;
  }>,
  overrides: { canSendReply?: boolean; hasBlockedOtherParticipant?: boolean } = {},
) {
  return {
    conversationId,
    otherParticipantId: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
    otherParticipantDisplayName: 'Bob',
    messages,
    page: 1,
    pageSize: 50,
    totalCount: messages.length,
    totalPages: 1,
    detailPath: `/messages/${conversationId}`,
    canSendReply: overrides.canSendReply ?? true,
    hasBlockedOtherParticipant: overrides.hasBlockedOtherParticipant ?? false,
  };
}

describe('ConversationScreen', () => {
  beforeEach(() => {
    mockSession.isSignedIn = true;
    mockSession.accessToken = 'tok';
    mockSession.profile = { memberId: 'member-1' } as never;
    fetchConversationMock.mockReset();
    replyToConversationMock.mockReset();
    reportConversationMessageMock.mockReset();
    archiveConversationMock.mockReset();
    blockConversationParticipantMock.mockReset();
    getMessagesCacheMock.mockReset();
    getMessagesCacheMock.mockReturnValue(
      fakeMessagesCache() as unknown as ReturnType<typeof getMessagesCache>,
    );
  });

  afterEach(async () => {
    await flushVirtualizedList();
  });

  it('renders markup and URLs as plain text', async () => {
    const body = '<script>alert(1)</script> https://example.com';
    fetchConversationMock.mockResolvedValue(
      conversationDetail([
        {
          id: theirMessageId,
          senderMemberId: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
          senderDisplayName: 'Bob',
          body,
          createdAt: '2026-08-19T12:00:00.000Z',
          isMine: false,
          sortKey: 1,
          reportedByViewer: false,
        },
      ]),
    );

    renderConversation();
    await waitFor(() => expect(screen.getByText(body)).toBeOnTheScreen(), { timeout: 8000 });
    expect(screen.queryByRole('link')).toBeNull();
  });

  it('lets a member report someone else\'s message with an optional reason', async () => {
    fetchConversationMock.mockResolvedValue(
      conversationDetail([
        {
          id: theirMessageId,
          senderMemberId: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
          senderDisplayName: 'Bob',
          body: 'Abusive text',
          createdAt: '2026-08-19T12:00:00.000Z',
          isMine: false,
          sortKey: 1,
          reportedByViewer: false,
        },
        {
          id: '22222222-3333-4444-5555-666666666666',
          senderMemberId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
          senderDisplayName: 'Alice',
          body: 'My reply',
          createdAt: '2026-08-19T12:01:00.000Z',
          isMine: true,
          sortKey: 2,
          reportedByViewer: false,
        },
      ]),
    );
    reportConversationMessageMock.mockResolvedValue({
      reportId: '99999999-aaaa-bbbb-cccc-dddddddddddd',
      alreadyReported: false,
    });

    renderConversation();
    await waitFor(() => expect(screen.getByText('Abusive text')).toBeOnTheScreen(), { timeout: 8000 });
    expect(screen.queryByRole('button', { name: 'Report message' })).toBeOnTheScreen();
    expect(screen.getByText('My reply')).toBeOnTheScreen();

    const user = userEvent.setup();
    await user.press(screen.getByRole('button', { name: 'Report message' }));
    await user.type(screen.getByLabelText('Optional reason'), 'Harassment');
    await user.press(screen.getByRole('button', { name: 'Submit report' }));

    await waitFor(() => expect(screen.getByText('REPORTED')).toBeOnTheScreen());
    expect(reportConversationMessageMock).toHaveBeenCalledWith(
      'tok',
      conversationId,
      theirMessageId,
      'Harassment',
    );
  });

  it('shows an error block with retry when the fetch fails', async () => {
    fetchConversationMock.mockRejectedValueOnce(new ApiError(500, 'The server had a problem.'));
    fetchConversationMock.mockResolvedValueOnce(conversationDetail([]));

    renderConversation();
    await waitFor(() => expect(screen.getByText('The server had a problem.')).toBeOnTheScreen());

    const user = userEvent.setup();
    await user.press(screen.getByRole('button', { name: 'Try again' }));
    await waitFor(() => expect(fetchConversationMock).toHaveBeenCalledTimes(2));
  });

  it('shows the generic sending-blocked notice when the reply composer is hidden', async () => {
    fetchConversationMock.mockResolvedValue(
      conversationDetail(
        [
          {
            id: theirMessageId,
            senderMemberId: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
            senderDisplayName: 'Bob',
            body: 'Hello',
            createdAt: '2026-08-19T12:00:00.000Z',
            isMine: false,
            sortKey: 1,
            reportedByViewer: false,
          },
        ],
        { canSendReply: false },
      ),
    );

    renderConversation();
    await waitFor(() => expect(screen.getByText('Unable to send message.')).toBeOnTheScreen());
    expect(screen.queryByLabelText('Reply')).toBeNull();
  });

  it('shows the you-blocked-this-member notice ahead of the generic notice', async () => {
    fetchConversationMock.mockResolvedValue(
      conversationDetail(
        [
          {
            id: theirMessageId,
            senderMemberId: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
            senderDisplayName: 'Bob',
            body: 'Hello',
            createdAt: '2026-08-19T12:00:00.000Z',
            isMine: false,
            sortKey: 1,
            reportedByViewer: false,
          },
        ],
        { canSendReply: false, hasBlockedOtherParticipant: true },
      ),
    );

    renderConversation();
    await waitFor(() =>
      expect(
        screen.getByText('You have blocked this member. They can no longer send you private messages.'),
      ).toBeOnTheScreen(),
    );
    expect(screen.queryByText('Unable to send message.')).toBeNull();
  });

  it('sends a reply and clears the draft', async () => {
    fetchConversationMock.mockResolvedValue(conversationDetail([]));
    replyToConversationMock.mockResolvedValue(
      conversationDetail([
        {
          id: '33333333-4444-5555-6666-777777777777',
          senderMemberId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
          senderDisplayName: 'Alice',
          body: 'Hello Bob',
          createdAt: '2026-08-19T12:02:00.000Z',
          isMine: true,
          sortKey: 1,
          reportedByViewer: false,
        },
      ]),
    );

    renderConversation();
    await waitFor(() => expect(screen.getByLabelText('Reply')).toBeOnTheScreen());

    const user = userEvent.setup();
    await user.type(screen.getByLabelText('Reply'), 'Hello Bob');
    await user.press(screen.getByRole('button', { name: 'Send reply' }));

    await waitFor(() => expect(screen.getByText('Hello Bob')).toBeOnTheScreen());
    expect(replyToConversationMock).toHaveBeenCalledWith('tok', conversationId, 'Hello Bob');
    expect(screen.getByLabelText('Reply').props.value).toBe('');
  });

  it('disables the send button until a reply is drafted', async () => {
    fetchConversationMock.mockResolvedValue(conversationDetail([]));

    renderConversation();
    await waitFor(() => expect(screen.getByLabelText('Reply')).toBeOnTheScreen());

    expect(screen.getByRole('button', { name: 'Send reply' })).toBeDisabled();

    const user = userEvent.setup();
    await user.type(screen.getByLabelText('Reply'), 'Hi');
    expect(screen.getByRole('button', { name: 'Send reply' })).toBeEnabled();

    expect(replyToConversationMock).not.toHaveBeenCalled();
  });

  it('archives the conversation and returns to the inbox', async () => {
    const alertSpy = jest.spyOn(Alert, 'alert').mockImplementation((_title, _message, buttons) => {
      buttons?.find((button) => button.text === 'Archive')?.onPress?.();
    });
    const navigation = fakeNavigation();
    fetchConversationMock.mockResolvedValue(
      conversationDetail([
        {
          id: theirMessageId,
          senderMemberId: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
          senderDisplayName: 'Bob',
          body: 'Hello',
          createdAt: '2026-08-19T12:00:00.000Z',
          isMine: false,
          sortKey: 1,
          reportedByViewer: false,
        },
      ]),
    );
    archiveConversationMock.mockResolvedValueOnce(undefined);

    renderWithProviders(
      <ConversationScreen
        navigation={navigation as never}
        route={{ key: 'conversation', name: 'Conversation', params: { id: conversationId } } as never}
      />,
    );
    await waitFor(() => expect(screen.getByText('Hello')).toBeOnTheScreen());

    const user = userEvent.setup();
    await user.press(screen.getByRole('button', { name: 'Archive conversation' }));

    await waitFor(() => expect(archiveConversationMock).toHaveBeenCalledWith('tok', conversationId));
    expect(navigation.navigate).toHaveBeenCalledWith('Inbox');
    alertSpy.mockRestore();
  });

  it('blocks the other participant from the overflow menu and shows the blocked notice', async () => {
    const alertSpy = jest.spyOn(Alert, 'alert').mockImplementation((_title, _message, buttons) => {
      const target =
        buttons?.find((button) => button.text === 'Block member' || button.text === 'Block') ??
        buttons?.find((button) => button.style !== 'cancel');
      target?.onPress?.();
    });
    const navigation = fakeNavigation();
    fetchConversationMock.mockResolvedValueOnce(
      conversationDetail([
        {
          id: theirMessageId,
          senderMemberId: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
          senderDisplayName: 'Bob',
          body: 'Hello',
          createdAt: '2026-08-19T12:00:00.000Z',
          isMine: false,
          sortKey: 1,
          reportedByViewer: false,
        },
      ]),
    );
    fetchConversationMock.mockResolvedValue(
      conversationDetail(
        [
          {
            id: theirMessageId,
            senderMemberId: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
            senderDisplayName: 'Bob',
            body: 'Hello',
            createdAt: '2026-08-19T12:00:00.000Z',
            isMine: false,
            sortKey: 1,
            reportedByViewer: false,
          },
        ],
        { canSendReply: false, hasBlockedOtherParticipant: true },
      ),
    );
    blockConversationParticipantMock.mockResolvedValue(undefined);

    const main = renderWithProviders(
      <ConversationScreen
        navigation={navigation as never}
        route={{ key: 'conversation', name: 'Conversation', params: { id: conversationId } } as never}
      />,
    );
    await waitFor(() => expect(main.getByText('Hello')).toBeOnTheScreen());

    // The native-stack header isn't mounted by this harness (navigation is a
    // jest.fn() stub), so render the headerRight element it was configured
    // with directly to reach the overflow button.
    const latestOptions = navigation.setOptions.mock.calls.at(-1)?.[0];
    const header = renderWithProviders(latestOptions.headerRight(), { navigation: false });

    const user = userEvent.setup();
    await user.press(header.getByRole('button', { name: 'More options' }));

    await waitFor(() =>
      expect(blockConversationParticipantMock).toHaveBeenCalledWith('tok', conversationId),
    );
    // The composer (which needs canSendReply) disappears once the reload
    // reflects the block, and the blocked notice takes its place.
    await waitFor(
      () => expect(main.queryByLabelText('Reply')).toBeNull(),
      { timeout: 8000 },
    );
    await waitFor(
      () =>
        expect(
          main.queryByText('You have blocked this member. They can no longer send you private messages.'),
        ).not.toBeNull(),
      { timeout: 8000 },
    );
    alertSpy.mockRestore();
  }, 15000);

  it('persists the freshly loaded conversation to the cache', async () => {
    const cache = fakeMessagesCache();
    getMessagesCacheMock.mockReturnValue(cache as unknown as ReturnType<typeof getMessagesCache>);
    fetchConversationMock.mockResolvedValue(
      conversationDetail([
        {
          id: theirMessageId,
          senderMemberId: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
          senderDisplayName: 'Bob',
          body: 'Hello',
          createdAt: '2026-08-19T12:00:00.000Z',
          isMine: false,
          sortKey: 1,
          reportedByViewer: false,
        },
      ]),
    );

    renderConversation();
    await waitFor(() => expect(screen.getByText('Hello')).toBeOnTheScreen());
    await waitFor(() =>
      expect(cache.put).toHaveBeenCalledWith(
        `conversation:member-1:${conversationId}`,
        expect.objectContaining({ conversationId }),
      ),
    );
  });

  it('renders the cached conversation instantly while the fresh fetch is still in flight', async () => {
    const cache = fakeMessagesCache();
    cache.get.mockResolvedValue(
      conversationDetail([
        {
          id: theirMessageId,
          senderMemberId: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
          senderDisplayName: 'Bob',
          body: 'From last visit',
          createdAt: '2026-08-19T12:00:00.000Z',
          isMine: false,
          sortKey: 1,
          reportedByViewer: false,
        },
      ]),
    );
    getMessagesCacheMock.mockReturnValue(cache as unknown as ReturnType<typeof getMessagesCache>);
    let resolveFetch: (value: Awaited<ReturnType<typeof fetchConversation>>) => void = () => {};
    fetchConversationMock.mockReturnValueOnce(
      new Promise((resolve) => {
        resolveFetch = resolve;
      }),
    );

    renderConversation();

    expect(cache.get).toHaveBeenCalledWith(`conversation:member-1:${conversationId}`);
    await waitFor(() => expect(screen.getByText('From last visit')).toBeOnTheScreen());

    resolveFetch(conversationDetail([]));
    await waitFor(() => expect(screen.queryByText('From last visit')).toBeNull());
  });
});
