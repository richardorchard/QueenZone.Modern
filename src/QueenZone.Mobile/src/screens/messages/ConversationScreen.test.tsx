import { screen, waitFor } from '@testing-library/react-native';
import { fetchConversation } from '../../api/messages';
import { createMockSession } from '../../test/mockSession';
import { fakeNavigation, renderWithProviders } from '../../test/render';
import { ConversationScreen } from './ConversationScreen';

const conversationId = 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee';
const mockSession = createMockSession();

jest.mock('../../session/SessionContext', () => ({
  useSession: () => mockSession,
}));

jest.mock('../../api/messages', () => ({
  fetchConversation: jest.fn(),
  replyToConversation: jest.fn(),
}));

const fetchConversationMock = fetchConversation as jest.MockedFunction<typeof fetchConversation>;

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
  });

  it('renders markup and URLs as plain text', async () => {
    const body = '<script>alert(1)</script> https://example.com';
    fetchConversationMock.mockResolvedValue({
      conversationId,
      otherParticipantId: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
      otherParticipantDisplayName: 'Bob',
      messages: [
        {
          id: '11111111-2222-3333-4444-555555555555',
          senderMemberId: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
          senderDisplayName: 'Bob',
          body,
          createdAt: '2026-08-19T12:00:00.000Z',
          isMine: false,
          sortKey: 1,
        },
      ],
      page: 1,
      pageSize: 50,
      totalCount: 1,
      totalPages: 1,
      detailPath: `/messages/${conversationId}`,
      canSendReply: true,
    });

    renderConversation();
    await waitFor(() => expect(screen.getByText(body)).toBeOnTheScreen());
    expect(screen.queryByRole('link')).toBeNull();
  });
});
