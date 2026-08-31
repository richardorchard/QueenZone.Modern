import { act, screen, userEvent, waitFor } from '@testing-library/react-native';
import { ApiError } from '../../api/client';
import { composeMessage, searchRecipients } from '../../api/messages';
import { conversationDetailFixture, memberProfileFixture } from '../../test/fixtures';
import { createMockSession } from '../../test/mockSession';
import { fakeNavigation, flushVirtualizedList, renderWithProviders } from '../../test/render';
import { ComposeMessageScreen } from './ComposeMessageScreen';

const mockSession = createMockSession();
const recipient = { memberId: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', displayName: 'Bob' };
const conversationId = 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee';

jest.mock('../../session/SessionContext', () => ({
  useSession: () => mockSession,
}));

jest.mock('../../api/messages', () => ({
  searchRecipients: jest.fn(),
  composeMessage: jest.fn(),
}));

jest.mock('../../offlineQueue', () => ({
  enqueueMessageCompose: jest.fn(async (input: { body: string; memberId: string; recipientMemberId: string }) => ({
    operationId: 'op-compose',
    payload: { body: input.body },
    memberId: input.memberId,
    kind: 'message.compose',
    target: { recipientMemberId: input.recipientMemberId },
  })),
  removeOfflineItem: jest.fn(),
  flushOfflineQueue: jest.fn(),
}));

const searchRecipientsMock = searchRecipients as jest.MockedFunction<typeof searchRecipients>;
const composeMessageMock = composeMessage as jest.MockedFunction<typeof composeMessage>;

function renderCompose(navigation = fakeNavigation()) {
  return {
    navigation,
    ...renderWithProviders(
      <ComposeMessageScreen
        navigation={navigation as never}
        route={{ key: 'compose', name: 'ComposeMessage' } as never}
      />,
    ),
  };
}

async function pickRecipient(user: ReturnType<typeof userEvent.setup>) {
  await user.type(screen.getByLabelText('Recipient search'), 'Bob');
  await waitFor(() => expect(screen.getByRole('button', { name: 'Message Bob' })).toBeOnTheScreen());
  await user.press(screen.getByRole('button', { name: 'Message Bob' }));
  await waitFor(() => expect(screen.getByText('Bob')).toBeOnTheScreen());
}

describe('ComposeMessageScreen', () => {
  beforeEach(() => {
    mockSession.isSignedIn = true;
    mockSession.accessToken = 'tok';
    mockSession.profile = memberProfileFixture({ memberId: 'member-1' });
    searchRecipientsMock.mockReset();
    composeMessageMock.mockReset();
    searchRecipientsMock.mockResolvedValue([recipient]);
    composeMessageMock.mockResolvedValue(conversationDetailFixture({ conversationId }));
  });

  afterEach(async () => {
    await flushVirtualizedList();
  });

  it('debounces recipient search by 250ms', async () => {
    jest.useFakeTimers();
    try {
      const user = userEvent.setup({ advanceTimers: jest.advanceTimersByTime });
      renderCompose();
      await waitFor(() => expect(screen.getByLabelText('Recipient search')).toBeOnTheScreen());
      await user.type(screen.getByLabelText('Recipient search'), 'Bob');
      expect(searchRecipientsMock).not.toHaveBeenCalled();

      await act(async () => {
        jest.advanceTimersByTime(249);
      });
      expect(searchRecipientsMock).not.toHaveBeenCalled();

      await act(async () => {
        jest.advanceTimersByTime(1);
      });
      await waitFor(() =>
        expect(searchRecipientsMock).toHaveBeenCalledWith('tok', 'Bob', expect.any(AbortSignal)),
      );
    } finally {
      jest.useRealTimers();
    }
  });

  it('picks a recipient, sends, and replaces with Conversation', async () => {
    const { navigation } = renderCompose();
    await waitFor(() => expect(screen.getByLabelText('Recipient search')).toBeOnTheScreen());

    const user = userEvent.setup();
    await pickRecipient(user);
    await user.type(screen.getByLabelText('Message body'), 'Hello Bob');
    await user.press(screen.getByRole('button', { name: 'Send message' }));

    await waitFor(() =>
      expect(composeMessageMock).toHaveBeenCalledWith(
        'tok',
        recipient.memberId,
        'Hello Bob',
        undefined,
        'op-compose',
      ),
    );
    expect(navigation.replace).toHaveBeenCalledWith('Conversation', { id: conversationId });
  });

  it('does not send when the body is empty', async () => {
    renderCompose();
    await waitFor(() => expect(screen.getByLabelText('Recipient search')).toBeOnTheScreen());

    const user = userEvent.setup();
    await pickRecipient(user);
    await user.press(screen.getByRole('button', { name: 'Send message' }));

    await waitFor(() => expect(screen.getByText('Message body is required.')).toBeOnTheScreen());
    expect(composeMessageMock).not.toHaveBeenCalled();
  });

  it('does not send when no recipient is chosen', async () => {
    renderCompose();
    await waitFor(() => expect(screen.getByLabelText('Message body')).toBeOnTheScreen());

    const user = userEvent.setup();
    await user.type(screen.getByLabelText('Message body'), 'Hello');
    expect(screen.getByRole('button', { name: 'Send message' })).toBeDisabled();
    await user.press(screen.getByRole('button', { name: 'Send message' }));

    expect(composeMessageMock).not.toHaveBeenCalled();
  });

  it('keeps the composer on screen when send fails', async () => {
    composeMessageMock.mockRejectedValueOnce(new ApiError(500, 'The server had a problem.'));
    const { navigation } = renderCompose();
    await waitFor(() => expect(screen.getByLabelText('Recipient search')).toBeOnTheScreen());

    const user = userEvent.setup();
    await pickRecipient(user);
    await user.type(screen.getByLabelText('Message body'), 'Hello Bob');
    await user.press(screen.getByRole('button', { name: 'Send message' }));

    await waitFor(() => expect(screen.getByText('The server had a problem.')).toBeOnTheScreen());
    expect(navigation.replace).not.toHaveBeenCalled();
    expect(screen.getByRole('button', { name: 'Send message' })).toBeOnTheScreen();
  });
});
