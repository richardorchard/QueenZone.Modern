import { fireEvent, screen, userEvent, waitFor, within } from '@testing-library/react-native';
import {
  fetchForumTopic,
  fetchForumTopicPoll,
  fetchForumTopicPosts,
  fetchForumTopicWatch,
  openForumAttachmentFile,
  openForumAttachmentImage,
  unwatchForumTopic,
  voteForumTopicPoll,
  watchForumTopic,
} from '../../api';
import { ApiError } from '../../api/client';
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
    openForumAttachmentFile: jest.fn(),
    openForumAttachmentImage: jest.fn(),
  };
});

const mockSession = createMockSession();

jest.mock('../../session/SessionContext', () => ({
  useSession: () => mockSession,
}));

jest.mock('../../config', () => ({
  getAppConfig: () => ({ apiBaseUrl: 'http://qz.test', appEnv: 'development', version: '0.1.0' }),
}));

const fetchTopic = fetchForumTopic as jest.MockedFunction<typeof fetchForumTopic>;
const fetchPosts = fetchForumTopicPosts as jest.MockedFunction<typeof fetchForumTopicPosts>;
const fetchPoll = fetchForumTopicPoll as jest.MockedFunction<typeof fetchForumTopicPoll>;
const fetchWatch = fetchForumTopicWatch as jest.MockedFunction<typeof fetchForumTopicWatch>;
const watchTopic = watchForumTopic as jest.MockedFunction<typeof watchForumTopic>;
const unwatchTopic = unwatchForumTopic as jest.MockedFunction<typeof unwatchForumTopic>;
const votePoll = voteForumTopicPoll as jest.MockedFunction<typeof voteForumTopicPoll>;
const openAttachment = openForumAttachmentFile as jest.MockedFunction<typeof openForumAttachmentFile>;
const openImage = openForumAttachmentImage as jest.MockedFunction<typeof openForumAttachmentImage>;

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
    votePoll.mockReset();
    openAttachment.mockReset();
    openAttachment.mockResolvedValue(undefined);
    openImage.mockReset();
    openImage.mockResolvedValue('data:image/jpeg;base64,dGVzdA==');
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

  it('shows a topic error and retries', async () => {
    fetchTopic.mockRejectedValueOnce(new ApiError(500, 'The server had a problem.'));
    fetchTopic.mockResolvedValueOnce({
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
    renderThread();
    await waitFor(() => expect(screen.getByText('The server had a problem.')).toBeOnTheScreen());

    const user = userEvent.setup();
    await user.press(screen.getByRole('button', { name: 'Try again' }));
    await waitFor(() => expect(fetchTopic).toHaveBeenCalledTimes(2));
    await waitFor(() => expect(screen.getByText('brightonrock')).toBeOnTheScreen());
  });

  it('hides Reply when the topic is locked', async () => {
    fetchTopic.mockResolvedValue({
      id: 1002,
      title: 'Ranking every studio album',
      forumId: 1,
      forumName: 'The Music',
      categoryPath: '/forum/1/the-music',
      detailPath: '/forum/topic/1002/ranking-every-studio-album',
      postCount: 1,
      hasPoll: false,
      isLocked: true,
    });
    renderThread();
    await waitFor(() => expect(screen.getByText('This topic is locked.')).toBeOnTheScreen());
    expect(screen.queryByRole('button', { name: 'Reply' })).toBeNull();
    expect(screen.queryByRole('button', { name: 'Sign in to reply' })).toBeNull();
  });

  it('opens the composer from the Reply CTA', async () => {
    mockSession.isSignedIn = true;
    mockSession.accessToken = 'tok';
    const { navigation } = renderThread();
    await waitFor(() => expect(screen.getByRole('button', { name: 'Reply' })).toBeOnTheScreen());
    expect(screen.getByTestId(testIds.forumThreadReply)).toBeOnTheScreen();

    const user = userEvent.setup();
    await user.press(screen.getByRole('button', { name: 'Reply' }));
    expect(navigation.navigate).toHaveBeenCalledWith('Composer', {
      threadId: 1002,
      threadTitle: 'Ranking every studio album',
      isLocked: false,
    });
  });

  it('wires a poll vote to voteForumTopicPoll', async () => {
    mockSession.isSignedIn = true;
    mockSession.accessToken = 'tok';
    fetchTopic.mockResolvedValue({
      id: 1002,
      title: 'Ranking every studio album',
      forumId: 1,
      forumName: 'The Music',
      categoryPath: '/forum/1/the-music',
      detailPath: '/forum/topic/1002/ranking-every-studio-album',
      postCount: 1,
      hasPoll: true,
      isLocked: false,
    });
    const openPoll = {
      pollId: 'poll-1',
      topicId: 1002,
      question: 'Best studio album?',
      isMultiChoice: false,
      maxChoices: null,
      closesAt: null,
      closedAt: null,
      createdAt: '2024-01-01T00:00:00.000Z',
      totalVotes: 1,
      distinctVoters: 1,
      viewerHasVoted: true,
      isClosed: false,
      canViewerVote: false,
      canViewerClose: false,
      options: [
        {
          optionId: 'opt-a',
          optionText: 'A Night at the Opera',
          displayOrder: 1,
          voteCount: 1,
          percentage: 100,
          selectedByViewer: true,
        },
        {
          optionId: 'opt-b',
          optionText: 'Innuendo',
          displayOrder: 2,
          voteCount: 0,
          percentage: 0,
          selectedByViewer: false,
        },
      ],
    };
    fetchPoll.mockResolvedValue({
      ...openPoll,
      totalVotes: 0,
      distinctVoters: 0,
      viewerHasVoted: false,
      canViewerVote: true,
      options: openPoll.options.map((option) => ({
        ...option,
        voteCount: 0,
        percentage: 0,
        selectedByViewer: false,
      })),
    });
    votePoll.mockResolvedValue(openPoll);
    renderThread();
    await waitFor(() => expect(screen.getByText('Best studio album?')).toBeOnTheScreen());

    const user = userEvent.setup();
    await user.press(screen.getByRole('radio', { name: 'A Night at the Opera' }));
    await user.press(screen.getByRole('button', { name: 'Vote' }));
    await waitFor(() => expect(votePoll).toHaveBeenCalledWith(1002, ['opt-a'], 'tok'));
  });
});

describe('ThreadScreen attachments', () => {
  const imageNoThumb = {
    fileName: 'anoto-setlist-scan.jpg',
    url: '/forum/attachment/legacy/1002',
    downloadUrl: '/api/v1/forum/attachments/legacy/1002',
    extension: 'JPG',
    formattedSize: '129.1 KB',
    isImage: true,
    thumbnailUrl: null,
  };
  const pdf = {
    fileName: 'opera-side-two-notes.pdf',
    url: '/forum/attachment/legacy/1101',
    downloadUrl: '/api/v1/forum/attachments/legacy/1101',
    extension: 'PDF',
    formattedSize: '47.0 KB',
    isImage: false,
    thumbnailUrl: null,
  };

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
    fetchPoll.mockResolvedValue({} as never);
    fetchWatch.mockResolvedValue({ watching: false });
    openAttachment.mockReset();
    openAttachment.mockResolvedValue(undefined);
    openImage.mockReset();
    openImage.mockResolvedValue('data:image/jpeg;base64,dGVzdA==');
  });

  afterEach(async () => {
    await flushVirtualizedList();
  });

  it('lets a signed-in member open an image with no thumbnail in the in-app viewer', async () => {
    mockSession.isSignedIn = true;
    mockSession.accessToken = 'tok';
    fetchPosts.mockResolvedValue(
      pagedResponse(
        [
          {
            id: 1002,
            body: '<p>Hello</p>',
            postedAt: '2024-06-01T10:00:00.000Z',
            authorUsername: 'brightonrock',
            signature: null,
            authorMemberSince: null,
            authorMemberId: null,
            editedAt: null,
            editCount: 0,
            attachments: [imageNoThumb],
          },
        ],
        1,
        1,
      ),
    );
    renderThread();
    await waitFor(() => expect(screen.getByText('anoto-setlist-scan.jpg')).toBeOnTheScreen());
    expect(screen.queryByTestId(testIds.forumThreadAttachmentViewer)).toBeNull();
    expect(openImage).not.toHaveBeenCalled();

    const user = userEvent.setup();
    await user.press(screen.getByRole('button', { name: /anoto-setlist-scan.jpg/ }));
    await waitFor(() =>
      expect(openImage).toHaveBeenCalledWith('/api/v1/forum/attachments/legacy/1002', 'tok'),
    );
    const viewer = screen.getByTestId(testIds.forumThreadAttachmentViewer);
    const viewerImage = within(viewer).getByLabelText('anoto-setlist-scan.jpg');
    expect(viewerImage.props.source).toEqual({
      uri: 'data:image/jpeg;base64,dGVzdA==',
    });
    expect(viewerImage.props.source.headers).toBeUndefined();
    expect(viewerImage.props.source.uri).not.toContain('/forum/attachment/');
    expect(viewerImage.props.source.uri).not.toMatch(/^https?:\/\//);
    expect(openAttachment).not.toHaveBeenCalled();
  });

  it('keeps a thumbed image inline and does not fetch the original', async () => {
    mockSession.isSignedIn = true;
    mockSession.accessToken = 'tok';
    fetchPosts.mockResolvedValue(
      pagedResponse(
        [
          {
            id: 1002,
            body: '<p>Hello</p>',
            postedAt: '2024-06-01T10:00:00.000Z',
            authorUsername: 'brightonrock',
            signature: null,
            authorMemberSince: null,
            authorMemberId: null,
            editedAt: null,
            editCount: 0,
            attachments: [
              {
                ...imageNoThumb,
                fileName: 'tour-poster.jpg',
                thumbnailUrl: '/ugc/forum/tour-poster-thumb.webp',
              },
            ],
          },
        ],
        1,
        1,
      ),
    );
    renderThread();
    await waitFor(() => expect(screen.getByText('tour-poster.jpg')).toBeOnTheScreen());
    expect(screen.getByLabelText('tour-poster.jpg')).toBeOnTheScreen();
    expect(screen.queryByRole('button', { name: /tour-poster.jpg/ })).toBeNull();
    expect(screen.queryByTestId(testIds.forumThreadAttachmentViewer)).toBeNull();
    expect(openImage).not.toHaveBeenCalled();
    expect(openAttachment).not.toHaveBeenCalled();
  });

  it('shows an error when the image download fails', async () => {
    mockSession.isSignedIn = true;
    mockSession.accessToken = 'tok';
    openImage.mockRejectedValueOnce(new ApiError(401, 'Sign in to continue.'));
    fetchPosts.mockResolvedValue(
      pagedResponse(
        [
          {
            id: 1002,
            body: '<p>Hello</p>',
            postedAt: '2024-06-01T10:00:00.000Z',
            authorUsername: 'brightonrock',
            signature: null,
            authorMemberSince: null,
            authorMemberId: null,
            editedAt: null,
            editCount: 0,
            attachments: [imageNoThumb],
          },
        ],
        1,
        1,
      ),
    );
    renderThread();
    await waitFor(() => expect(screen.getByText('anoto-setlist-scan.jpg')).toBeOnTheScreen());

    const user = userEvent.setup();
    await user.press(screen.getByRole('button', { name: /anoto-setlist-scan.jpg/ }));
    await waitFor(() => expect(screen.getByText('Sign in to continue.')).toBeOnTheScreen());
    expect(screen.queryByTestId(testIds.forumThreadAttachmentViewer)).toBeNull();
  });

  it('lets a signed-in member download a non-image attachment', async () => {
    mockSession.isSignedIn = true;
    mockSession.accessToken = 'tok';
    fetchPosts.mockResolvedValue(
      pagedResponse(
        [
          {
            id: 1101,
            body: '<p>Notes</p>',
            postedAt: '2024-06-01T11:00:00.000Z',
            authorUsername: 'jazzfanz',
            signature: null,
            authorMemberSince: null,
            authorMemberId: null,
            editedAt: null,
            editCount: 0,
            attachments: [pdf],
          },
        ],
        1,
        1,
      ),
    );
    renderThread();
    await waitFor(() => expect(screen.getByText('opera-side-two-notes.pdf')).toBeOnTheScreen());

    const user = userEvent.setup();
    await user.press(screen.getByRole('button', { name: /opera-side-two-notes.pdf/ }));
    await waitFor(() =>
      expect(openAttachment).toHaveBeenCalledWith(
        '/api/v1/forum/attachments/legacy/1101',
        'tok',
        'opera-side-two-notes.pdf',
        { present: false },
      ),
    );
    expect(openAttachment.mock.calls[0]?.[0]).not.toContain('/forum/attachment/legacy/');
    expect(screen.getByTestId(testIds.forumThreadAttachmentOpened)).toBeOnTheScreen();
  });

  it('shows signed-out metadata without opening bytes', async () => {
    fetchPosts.mockResolvedValue(
      pagedResponse(
        [
          {
            id: 1002,
            body: '<p>Hello</p>',
            postedAt: '2024-06-01T10:00:00.000Z',
            authorUsername: 'brightonrock',
            signature: null,
            authorMemberSince: null,
            authorMemberId: null,
            editedAt: null,
            editCount: 0,
            attachments: [imageNoThumb, pdf],
          },
        ],
        1,
        1,
      ),
    );
    renderThread();
    await waitFor(() => expect(screen.getByText('anoto-setlist-scan.jpg')).toBeOnTheScreen());
    expect(screen.getByText('opera-side-two-notes.pdf')).toBeOnTheScreen();
    expect(screen.getAllByText(/Members only/).length).toBe(2);
    expect(screen.queryByRole('button', { name: /anoto-setlist-scan.jpg/ })).toBeNull();
    expect(screen.queryByRole('button', { name: /opera-side-two-notes.pdf/ })).toBeNull();
    expect(screen.queryByTestId(testIds.forumThreadAttachmentViewer)).toBeNull();
    expect(openAttachment).not.toHaveBeenCalled();
    expect(openImage).not.toHaveBeenCalled();
  });
});
