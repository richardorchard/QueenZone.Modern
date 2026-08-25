import { screen, userEvent, waitFor } from '@testing-library/react-native';
import { fetchConversation, reportConversationMessage } from '../../api/messages';
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
}));

const fetchConversationMock = fetchConversation as jest.MockedFunction<typeof fetchConversation>;
const reportConversationMessageMock = reportConversationMessage as jest.MockedFunction<
  typeof reportConversationMessage
>;

function renderConversation() {
  return renderWithProviders(
    <ConversationScreen
      navigation={fakeNavigation() as never}
      route={{ key: 'conversation', name: 'Conversation', params: { id: conversationId } } as never}
    />,
  );
}

describe('ConversationScreen', () => {
  beforeEach(() => {
    mockSession.isSignedIn = true;
    mockSession.accessToken = 'tok';
    fetchConversationMock.mockReset();
    reportConversationMessageMock.mockReset();
  });

  afterEach(async () => {
    await flushVirtualizedList();
  });

  it('lets a member report someone else\'s message with an optional reason', async () => {
    fetchConversationMock.mockResolvedValue({
      conversationId,
      otherParticipantId: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
      otherParticipantDisplayName: 'Bob',
      messages: [
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
      ],
      page: 1,
      pageSize: 50,
      totalCount: 2,
      totalPages: 1,
      detailPath: `/messages/${conversationId}`,
      canSendReply: true,
    });
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
});
