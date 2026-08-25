import { screen, userEvent, waitFor } from '@testing-library/react-native';
import { ApiError } from '../../api/client';
import { fetchConversation, replyToConversation, reportConversationMessage } from '../../api/messages';
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
const replyToConversationMock = replyToConversation as jest.MockedFunction<typeof replyToConversation>;
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

function conversationDetail(messages: Array<{
  id: string;
  senderMemberId: string;
  senderDisplayName: string;
  body: string;
  createdAt: string;
  isMine: boolean;
  sortKey: number;
  reportedByViewer?: boolean;
}>) {
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
    canSendReply: true,
  };
}

describe('ConversationScreen', () => {
  beforeEach(() => {
    mockSession.isSignedIn = true;
    mockSession.accessToken = 'tok';
    fetchConversationMock.mockReset();
    replyToConversationMock.mockReset();
    reportConversationMessageMock.mockReset();
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

  it('shows an error block with retry when the fetch fails', async () => {
    fetchConversationMock.mockRejectedValueOnce(new ApiError(500, 'The server had a problem.'));
    fetchConversationMock.mockResolvedValueOnce(conversationDetail([]));

    renderConversation();
    await waitFor(() => expect(screen.getByText('The server had a problem.')).toBeOnTheScreen());

    const user = userEvent.setup();
    await user.press(screen.getByRole('button', { name: 'Try again' }));
    await waitFor(() => expect(fetchConversationMock).toHaveBeenCalledTimes(2));
  });

  it('shows the unable-to-send notice instead of a composer when canSendReply is false', async () => {
    fetchConversationMock.mockResolvedValue({ ...conversationDetail([]), canSendReply: false });

    renderConversation();
    await waitFor(() => expect(screen.getByText('Unable to send message.')).toBeOnTheScreen());
    expect(screen.queryByLabelText('Reply')).toBeNull();
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

  it('rejects an empty reply without calling the API', async () => {
    fetchConversationMock.mockResolvedValue(conversationDetail([]));

    renderConversation();
    await waitFor(() => expect(screen.getByLabelText('Reply')).toBeOnTheScreen());

    const user = userEvent.setup();
    await user.press(screen.getByRole('button', { name: 'Send reply' }));

    await waitFor(() => expect(screen.getByText('Message body is required.')).toBeOnTheScreen());
    expect(replyToConversationMock).not.toHaveBeenCalled();
  });
});
