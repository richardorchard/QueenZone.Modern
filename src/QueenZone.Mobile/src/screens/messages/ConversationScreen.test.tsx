import { screen, userEvent, waitFor } from '@testing-library/react-native';
import { archiveConversation, fetchConversation, reportConversationMessage } from '../../api/messages';
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
}));

const fetchConversationMock = fetchConversation as jest.MockedFunction<typeof fetchConversation>;
const reportConversationMessageMock = reportConversationMessage as jest.MockedFunction<
  typeof reportConversationMessage
>;
const archiveConversationMock = archiveConversation as jest.MockedFunction<typeof archiveConversation>;

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
    fetchConversationMock.mockReset();
    reportConversationMessageMock.mockReset();
    archiveConversationMock.mockReset();
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
    await waitFor(() => expect(screen.getByText(body)).toBeOnTheScreen());
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

    await waitFor(() => expect(screen.getByText('Reported')).toBeOnTheScreen());
    expect(reportConversationMessageMock).toHaveBeenCalledWith(
      'tok',
      conversationId,
      theirMessageId,
      'Harassment',
    );
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

  it('archives the conversation and returns to the inbox', async () => {
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
  });
});
