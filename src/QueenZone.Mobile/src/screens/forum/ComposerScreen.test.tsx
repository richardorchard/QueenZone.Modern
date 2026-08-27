import { screen, userEvent, waitFor } from '@testing-library/react-native';
import { ApiError, createForumReply, createForumTopic, fetchForumCategories } from '../../api';
import { pagedResponse } from '../../test/fixtures';
import { createMockSession } from '../../test/mockSession';
import { fakeNavigation, renderWithProviders } from '../../test/render';
import { ComposerScreen } from './ComposerScreen';

const mockSession = createMockSession();

jest.mock('../../session/SessionContext', () => ({
  useSession: () => mockSession,
}));

jest.mock('../../api', () => {
  const actual = jest.requireActual('../../api');
  return {
    ...actual,
    createForumReply: jest.fn(),
    createForumTopic: jest.fn(),
    fetchForumCategories: jest.fn(),
  };
});

const createForumReplyMock = createForumReply as jest.MockedFunction<typeof createForumReply>;
const createForumTopicMock = createForumTopic as jest.MockedFunction<typeof createForumTopic>;
const fetchForumCategoriesMock = fetchForumCategories as jest.MockedFunction<typeof fetchForumCategories>;

function renderComposer(
  params: {
    threadId?: number;
    threadTitle?: string;
    categoryId?: number;
    categoryName?: string;
    isLocked?: boolean;
  } = {},
  navigation = fakeNavigation(),
) {
  return {
    navigation,
    ...renderWithProviders(
      <ComposerScreen
        navigation={navigation as never}
        route={{ key: 'composer', name: 'Composer', params } as never}
      />,
    ),
  };
}

describe('ComposerScreen', () => {
  beforeEach(() => {
    mockSession.isSignedIn = true;
    mockSession.accessToken = 'tok';
    createForumReplyMock.mockReset();
    createForumTopicMock.mockReset();
    fetchForumCategoriesMock.mockReset();
    fetchForumCategoriesMock.mockResolvedValue(
      pagedResponse([
        {
          id: 1,
          name: 'The Music',
          description: null,
          postCount: 10,
          lastActivityAt: null,
          latestThreadTitle: null,
          detailPath: '/forum/1/the-music',
        },
      ]),
    );
  });

  it('publishes a reply and goes back', async () => {
    createForumReplyMock.mockResolvedValueOnce({
      id: 88,
      topicId: 1002,
      detailPath: '/forum/topic/1002',
    });
    const { navigation } = renderComposer({ threadId: 1002, threadTitle: 'Ranking every studio album' });

    await waitFor(() => expect(screen.getByLabelText('Reply body')).toBeOnTheScreen());
    const user = userEvent.setup();
    await user.type(screen.getByLabelText('Reply body'), 'A reply from mobile');
    await user.press(screen.getByRole('button', { name: 'Post reply' }));

    await waitFor(() =>
      expect(createForumReplyMock).toHaveBeenCalledWith(1002, { body: 'A reply from mobile' }, 'tok'),
    );
    expect(navigation.goBack).toHaveBeenCalled();
    expect(createForumTopicMock).not.toHaveBeenCalled();
  });

  it('publishes a new topic and replaces with Thread', async () => {
    createForumTopicMock.mockResolvedValueOnce({
      id: 2001,
      starterPostId: 1,
      title: 'Fresh forum news',
      detailPath: '/forum/topic/2001/fresh-forum-news',
    });
    const { navigation } = renderComposer();

    await waitFor(() => expect(screen.getByRole('button', { name: 'Post to The Music' })).toBeOnTheScreen());
    const user = userEvent.setup();
    await user.press(screen.getByRole('button', { name: 'Post to The Music' }));
    await user.type(screen.getByLabelText('Topic title'), 'Fresh forum news');
    await user.type(screen.getByLabelText('Topic body'), 'Hello fans');
    await user.press(screen.getByRole('button', { name: 'Post topic' }));

    await waitFor(() =>
      expect(createForumTopicMock).toHaveBeenCalledWith(1, { title: 'Fresh forum news', body: 'Hello fans' }, 'tok'),
    );
    expect(navigation.replace).toHaveBeenCalledWith('Thread', { id: 2001, title: 'Fresh forum news' });
    expect(createForumReplyMock).not.toHaveBeenCalled();
  });

  it('surfaces an empty-body validation error without calling the API', async () => {
    renderComposer({ threadId: 1002, threadTitle: 'Ranking every studio album' });
    await waitFor(() => expect(screen.getByRole('button', { name: 'Post reply' })).toBeOnTheScreen());

    const user = userEvent.setup();
    await user.press(screen.getByRole('button', { name: 'Post reply' }));

    await waitFor(() => expect(screen.getByText('Write a post before publishing.')).toBeOnTheScreen());
    expect(createForumReplyMock).not.toHaveBeenCalled();
    expect(createForumTopicMock).not.toHaveBeenCalled();
  });

  it('does not call the API when the topic is locked', async () => {
    renderComposer({ threadId: 1002, threadTitle: 'Locked topic', isLocked: true });
    await waitFor(() => expect(screen.getByText('This topic is locked.')).toBeOnTheScreen());
    expect(screen.queryByRole('button', { name: 'Post reply' })).toBeNull();
    expect(createForumReplyMock).not.toHaveBeenCalled();
  });

  it('keeps the composer on screen when publish fails', async () => {
    createForumReplyMock.mockRejectedValueOnce(new ApiError(500, 'The server had a problem.'));
    const { navigation } = renderComposer({ threadId: 1002, threadTitle: 'Ranking every studio album' });

    await waitFor(() => expect(screen.getByLabelText('Reply body')).toBeOnTheScreen());
    const user = userEvent.setup();
    await user.type(screen.getByLabelText('Reply body'), 'A reply from mobile');
    await user.press(screen.getByRole('button', { name: 'Post reply' }));

    await waitFor(() => expect(screen.getByText('The server had a problem.')).toBeOnTheScreen());
    expect(navigation.goBack).not.toHaveBeenCalled();
    expect(screen.getByRole('button', { name: 'Post reply' })).toBeOnTheScreen();
  });
});
