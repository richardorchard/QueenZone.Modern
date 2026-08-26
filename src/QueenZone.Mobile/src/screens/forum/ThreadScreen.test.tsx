import { fireEvent, screen, waitFor } from '@testing-library/react-native';
import {
  fetchForumTopic,
  fetchForumTopicPoll,
  fetchForumTopicPosts,
  fetchForumTopicWatch,
  unwatchForumTopic,
  watchForumTopic,
} from '../../api';
import { pagedResponse } from '../../test/fixtures';
import { createMockSession } from '../../test/mockSession';
import { fakeNavigation, flushVirtualizedList, renderWithProviders } from '../../test/render';
import { testIds } from '../../test/testIds';
import { ThreadScreen } from './ThreadScreen';

jest.mock('../../api', () => {
  const actual = jest.requireActual('../../api');
  return {
    ...actual,
    fetchForumTopic: jest.fn(),
    fetchForumTopicPosts: jest.fn(),
    fetchForumTopicPoll: jest.fn(),
    fetchForumTopicWatch: jest.fn(),
    watchForumTopic: jest.fn(),
    unwatchForumTopic: jest.fn(),
    voteForumTopicPoll: jest.fn(),
    closeForumTopicPoll: jest.fn(),
  };
});

const mockSession = createMockSession();

jest.mock('../../session/SessionContext', () => ({
  useSession: () => mockSession,
}));

const fetchTopic = fetchForumTopic as jest.MockedFunction<typeof fetchForumTopic>;
const fetchPosts = fetchForumTopicPosts as jest.MockedFunction<typeof fetchForumTopicPosts>;
const fetchPoll = fetchForumTopicPoll as jest.MockedFunction<typeof fetchForumTopicPoll>;
const fetchWatch = fetchForumTopicWatch as jest.MockedFunction<typeof fetchForumTopicWatch>;
const watchTopic = watchForumTopic as jest.MockedFunction<typeof watchForumTopic>;
const unwatchTopic = unwatchForumTopic as jest.MockedFunction<typeof unwatchForumTopic>;

function renderThread(navigation = fakeNavigation()) {
  return {
    navigation,
    ...renderWithProviders(
      <ThreadScreen
        navigation={navigation as never}
        route={
          {
            key: 'thread',
            name: 'Thread',
            params: { id: 1002, title: 'Ranking every studio album' },
          } as never
        }
      />,
      { navigation: false },
    ),
  };
}

describe('ThreadScreen watch control', () => {
  beforeEach(() => {
    mockSession.isSignedIn = false;
    mockSession.accessToken = null;
    fetchTopic.mockResolvedValue({
      id: 1002,
      title: 'Ranking every studio album',
      forumId: 1,
      forumName: 'The Music',
      categoryPath: '/forum/1/the-music',
      detailPath: '/forum/topic/1002/ranking-every-studio-album',
      postCount: 1,
      hasPoll: false,
      isLocked: false,
    });
    fetchPosts.mockResolvedValue(
      pagedResponse(
        [
          {
            id: 1,
            body: '<p>Hello</p>',
            postedAt: '2024-06-01T10:00:00.000Z',
            authorUsername: 'brightonrock',
            signature: null,
            authorMemberSince: null,
            authorMemberId: null,
            editedAt: null,
            editCount: 0,
            attachments: [],
          },
        ],
        1,
        1,
      ),
    );
    fetchPoll.mockResolvedValue({} as never);
    fetchWatch.mockResolvedValue({ watching: false });
    watchTopic.mockResolvedValue({ watching: true });
    unwatchTopic.mockResolvedValue({ watching: false });
  });

  afterEach(async () => {
    await flushVirtualizedList();
  });

  it('asks signed-out members to sign in to watch', async () => {
    renderThread();
    await waitFor(() => expect(screen.getByTestId(testIds.forumThreadWatch)).toBeTruthy());
    expect(screen.getByLabelText('Sign in to watch')).toBeTruthy();
    expect(fetchWatch).not.toHaveBeenCalled();
  });

  it('watches and unwatches a topic for a signed-in member', async () => {
    mockSession.isSignedIn = true;
    mockSession.accessToken = 'tok';
    renderThread();

    await waitFor(() => expect(fetchWatch).toHaveBeenCalledWith(1002, 'tok', expect.any(AbortSignal)));
    const watchButton = await screen.findByLabelText('Watch topic');
    fireEvent.press(watchButton);

    await waitFor(() => expect(watchTopic).toHaveBeenCalledWith(1002, 'tok'));
    await screen.findByLabelText('Unwatch');
    fireEvent.press(screen.getByLabelText('Unwatch'));
    await waitFor(() => expect(unwatchTopic).toHaveBeenCalledWith(1002, 'tok'));
  });
});
