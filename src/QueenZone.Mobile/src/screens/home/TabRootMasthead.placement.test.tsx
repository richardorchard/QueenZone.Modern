import { screen, userEvent, waitFor } from '@testing-library/react-native';
import {
  fetchForumCategories,
  fetchForumRecentThreads,
  fetchForumTopic,
  fetchForumTopicPoll,
  fetchForumTopicPosts,
  fetchForumTopicWatch,
  fetchInbox,
  fetchLiveActivity,
  fetchNewsDetail,
  fetchNewsPage,
  fetchNewsYearRange,
  fetchOnThisDay,
  fetchPhotoCategories,
  fetchRandomQuote,
} from '../../api';
import { fetchConversation } from '../../api/messages';
import { newsDetailFixture, newsItemFixture, pagedResponse } from '../../test/fixtures';
import { createMockSession } from '../../test/mockSession';
import { fakeNavigation, flushVirtualizedList, renderWithProviders } from '../../test/render';
import { testIds } from '../../test/testIds';
import { ArchiveHubScreen } from '../archive/ArchiveHubScreen';
import { ForumScreen } from '../forum/ForumScreen';
import { ThreadScreen } from '../forum/ThreadScreen';
import { ConversationScreen } from '../messages/ConversationScreen';
import { NewsIndexScreen } from '../news/NewsIndexScreen';
import { NewsStoryScreen } from '../news/NewsStoryScreen';
import { PhotosScreen } from '../photos/PhotosScreen';
import { HomeScreen } from './HomeScreen';

jest.mock('../../api', () => {
  const actual = jest.requireActual('../../api');
  return {
    ...actual,
    fetchNewsPage: jest.fn(),
    fetchNewsYearRange: jest.fn(),
    fetchNewsDetail: jest.fn(),
    fetchForumRecentThreads: jest.fn(),
    fetchForumCategories: jest.fn(),
    fetchForumTopic: jest.fn(),
    fetchForumTopicPosts: jest.fn(),
    fetchForumTopicPoll: jest.fn(),
    fetchForumTopicWatch: jest.fn(),
    fetchPhotoCategories: jest.fn(),
    fetchOnThisDay: jest.fn(),
    fetchRandomQuote: jest.fn(),
    fetchLiveActivity: jest.fn(),
    fetchInbox: jest.fn(),
  };
});

jest.mock('../../api/messages', () => {
  const actual = jest.requireActual('../../api/messages');
  return {
    ...actual,
    fetchConversation: jest.fn(),
    fetchUnreadConversationCount: jest.fn(),
  };
});

const mockSession = createMockSession();

jest.mock('../../session/SessionContext', () => ({
  useSession: () => mockSession,
}));

jest.mock('../messages/useUnreadConversationCount', () => ({
  useUnreadConversationCount: () => 0,
}));

jest.mock('../../widgets/widgetSync', () => ({
  syncHomeWidget: jest.fn().mockResolvedValue(undefined),
}));

jest.mock('expo-linear-gradient', () => {
  const { View } = require('react-native');
  return { LinearGradient: View };
});

const fetchNews = fetchNewsPage as jest.MockedFunction<typeof fetchNewsPage>;
const fetchYearRange = fetchNewsYearRange as jest.MockedFunction<typeof fetchNewsYearRange>;
const fetchDetail = fetchNewsDetail as jest.MockedFunction<typeof fetchNewsDetail>;
const fetchForum = fetchForumRecentThreads as jest.MockedFunction<typeof fetchForumRecentThreads>;
const fetchForumCats = fetchForumCategories as jest.MockedFunction<typeof fetchForumCategories>;
const fetchTopic = fetchForumTopic as jest.MockedFunction<typeof fetchForumTopic>;
const fetchPosts = fetchForumTopicPosts as jest.MockedFunction<typeof fetchForumTopicPosts>;
const fetchPoll = fetchForumTopicPoll as jest.MockedFunction<typeof fetchForumTopicPoll>;
const fetchWatch = fetchForumTopicWatch as jest.MockedFunction<typeof fetchForumTopicWatch>;
const fetchPhotos = fetchPhotoCategories as jest.MockedFunction<typeof fetchPhotoCategories>;
const fetchDay = fetchOnThisDay as jest.MockedFunction<typeof fetchOnThisDay>;
const fetchQuote = fetchRandomQuote as jest.MockedFunction<typeof fetchRandomQuote>;
const fetchLive = fetchLiveActivity as jest.MockedFunction<typeof fetchLiveActivity>;
const fetchInboxMock = fetchInbox as jest.MockedFunction<typeof fetchInbox>;
const fetchConversationMock = fetchConversation as jest.MockedFunction<typeof fetchConversation>;

const conversationId = 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee';

describe('TabRootMasthead placement', () => {
  beforeEach(() => {
    mockSession.isSignedIn = false;
    mockSession.accessToken = null;
    fetchNews.mockResolvedValue(pagedResponse([newsItemFixture({ id: 1003 })], 1, 1));
    fetchYearRange.mockResolvedValue({ minYear: 2006, maxYear: 2026 });
    fetchDetail.mockResolvedValue(newsDetailFixture({ id: 1003, title: 'QueenZone modernisation begins' }));
    fetchForum.mockResolvedValue([]);
    fetchForumCats.mockResolvedValue(
      pagedResponse(
        [
          {
            id: 1,
            name: 'General',
            description: 'Talk about Queen.',
            postCount: 12,
            lastActivityAt: '2026-01-01T00:00:00.000Z',
            latestThreadTitle: 'Ranking every studio album',
            detailPath: '/forum/1/general',
          },
        ],
        1,
        1,
      ),
    );
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
    fetchPhotos.mockResolvedValue(pagedResponse([], 1, 0));
    fetchDay.mockResolvedValue(null);
    fetchQuote.mockResolvedValue(null);
    fetchLive.mockResolvedValue({ newForumRepliesToday: 0 });
    fetchInboxMock.mockResolvedValue(pagedResponse([], 1, 0));
    fetchConversationMock.mockResolvedValue({
      conversationId,
      otherParticipantId: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
      otherParticipantDisplayName: 'Bob',
      messages: [
        {
          id: '11111111-2222-3333-4444-555555555555',
          senderMemberId: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
          senderDisplayName: 'Bob',
          body: 'Hello',
          createdAt: '2026-01-01T00:00:00.000Z',
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
      hasBlockedOtherParticipant: false,
    });
  });

  afterEach(async () => {
    await flushVirtualizedList();
  });

  it('renders on Home, NewsIndex, PhotoIndex, ArchiveHub, and ForumIndex', async () => {
    const navigation = fakeNavigation();

    const home = renderWithProviders(
      <HomeScreen navigation={navigation as never} route={{ key: 'home', name: 'Home' } as never} />,
      { navigation: false },
    );
    expect(screen.getByTestId(testIds.tabMasthead)).toBeOnTheScreen();
    home.unmount();

    const news = renderWithProviders(
      <NewsIndexScreen
        navigation={navigation as never}
        route={{ key: 'news', name: 'NewsIndex' } as never}
      />,
      { navigation: false },
    );
    await waitFor(() => expect(screen.getByTestId(testIds.tabMasthead)).toBeOnTheScreen());
    news.unmount();

    const photos = renderWithProviders(
      <PhotosScreen
        navigation={navigation as never}
        route={{ key: 'photos', name: 'PhotoIndex' } as never}
      />,
      { navigation: false },
    );
    await waitFor(() => expect(screen.getByTestId(testIds.tabMasthead)).toBeOnTheScreen());
    photos.unmount();

    const archive = renderWithProviders(
      <ArchiveHubScreen
        navigation={navigation as never}
        route={{ key: 'archive', name: 'ArchiveHub' } as never}
      />,
      { navigation: false },
    );
    expect(screen.getByTestId(testIds.tabMasthead)).toBeOnTheScreen();
    archive.unmount();

    renderWithProviders(
      <ForumScreen
        navigation={navigation as never}
        route={{ key: 'forum', name: 'ForumIndex' } as never}
      />,
      { navigation: false },
    );
    await waitFor(() => expect(screen.getByTestId(testIds.tabMasthead)).toBeOnTheScreen());
  });

  it('opens search and Profile from each tab-root masthead', async () => {
    const user = userEvent.setup();
    const homeNav = fakeNavigation();
    const home = renderWithProviders(
      <HomeScreen navigation={homeNav as never} route={{ key: 'home', name: 'Home' } as never} />,
      { navigation: false },
    );
    await user.press(screen.getByTestId(testIds.homeSearch));
    await user.press(screen.getByTestId(testIds.homeProfile));
    expect(homeNav.navigate).toHaveBeenCalledWith('Search');
    expect(homeNav.navigate).toHaveBeenCalledWith('Profile');
    expect(screen.queryByTestId(testIds.homeMessages)).not.toBeOnTheScreen();
    home.unmount();

    const otherNav = fakeNavigation();
    const screens = [
      <NewsIndexScreen
        key="news"
        navigation={otherNav as never}
        route={{ key: 'news', name: 'NewsIndex' } as never}
      />,
      <PhotosScreen
        key="photos"
        navigation={otherNav as never}
        route={{ key: 'photos', name: 'PhotoIndex' } as never}
      />,
      <ArchiveHubScreen
        key="archive"
        navigation={otherNav as never}
        route={{ key: 'archive', name: 'ArchiveHub' } as never}
      />,
      <ForumScreen
        key="forum"
        navigation={otherNav as never}
        route={{ key: 'forum', name: 'ForumIndex' } as never}
      />,
    ];

    for (const screenNode of screens) {
      otherNav.navigate.mockClear();
      const view = renderWithProviders(screenNode, { navigation: false });
      await waitFor(() => expect(screen.getByTestId(testIds.tabMasthead)).toBeOnTheScreen());
      await user.press(screen.getByTestId(testIds.homeSearch));
      await user.press(screen.getByTestId(testIds.homeProfile));
      expect(otherNav.navigate).toHaveBeenCalledWith('Search');
      expect(otherNav.navigate).toHaveBeenCalledWith('HomeTab', { screen: 'Profile' });
      expect(screen.queryByTestId(testIds.homeMessages)).not.toBeOnTheScreen();
      view.unmount();
    }
  });

  it('opens Inbox from the signed-in messages icon on each tab-root masthead', async () => {
    mockSession.isSignedIn = true;
    mockSession.accessToken = 'tok';
    const user = userEvent.setup();

    const homeNav = fakeNavigation();
    const home = renderWithProviders(
      <HomeScreen navigation={homeNav as never} route={{ key: 'home', name: 'Home' } as never} />,
      { navigation: false },
    );
    await user.press(screen.getByTestId(testIds.homeMessages));
    expect(homeNav.navigate).toHaveBeenCalledWith('Inbox');
    home.unmount();

    const otherNav = fakeNavigation();
    const screens = [
      <NewsIndexScreen
        key="news"
        navigation={otherNav as never}
        route={{ key: 'news', name: 'NewsIndex' } as never}
      />,
      <PhotosScreen
        key="photos"
        navigation={otherNav as never}
        route={{ key: 'photos', name: 'PhotoIndex' } as never}
      />,
      <ArchiveHubScreen
        key="archive"
        navigation={otherNav as never}
        route={{ key: 'archive', name: 'ArchiveHub' } as never}
      />,
      <ForumScreen
        key="forum"
        navigation={otherNav as never}
        route={{ key: 'forum', name: 'ForumIndex' } as never}
      />,
    ];

    for (const screenNode of screens) {
      otherNav.navigate.mockClear();
      const view = renderWithProviders(screenNode, { navigation: false });
      await waitFor(() => expect(screen.getByTestId(testIds.tabMasthead)).toBeOnTheScreen());
      await user.press(screen.getByTestId(testIds.homeMessages));
      expect(otherNav.navigate).toHaveBeenCalledWith('HomeTab', { screen: 'Inbox' });
      view.unmount();
    }
  });

  it('is absent on Story, Thread, and Conversation', async () => {
    const navigation = fakeNavigation();

    const story = renderWithProviders(
      <NewsStoryScreen
        navigation={navigation as never}
        route={{ key: 'story', name: 'Story', params: { id: 1003 } } as never}
      />,
      { navigation: false },
    );
    await waitFor(() => expect(screen.getByTestId(testIds.newsStoryScreen)).toBeOnTheScreen());
    expect(screen.queryByTestId(testIds.tabMasthead)).not.toBeOnTheScreen();
    story.unmount();

    const thread = renderWithProviders(
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
    );
    await waitFor(() => expect(screen.getByTestId(testIds.forumThreadScreen)).toBeOnTheScreen());
    expect(screen.queryByTestId(testIds.tabMasthead)).not.toBeOnTheScreen();
    thread.unmount();

    mockSession.isSignedIn = true;
    mockSession.accessToken = 'tok';
    renderWithProviders(
      <ConversationScreen
        navigation={navigation as never}
        route={{ key: 'conversation', name: 'Conversation', params: { id: conversationId } } as never}
      />,
    );
    await waitFor(() => expect(screen.getByText('Hello')).toBeOnTheScreen());
    expect(screen.queryByTestId(testIds.tabMasthead)).not.toBeOnTheScreen();
  });
});
