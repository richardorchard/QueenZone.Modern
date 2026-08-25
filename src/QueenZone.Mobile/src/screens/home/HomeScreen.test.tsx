import { act, fireEvent, screen, userEvent, waitFor } from '@testing-library/react-native';
import { RefreshControl } from 'react-native';
import {
  fetchForumRecentThreads,
  fetchLiveActivity,
  fetchNewsPage,
  fetchOnThisDay,
  fetchPhotoCategories,
} from '../../api';
import { newsItemFixture, pagedResponse } from '../../test/fixtures';
import { createMockSession } from '../../test/mockSession';
import { fakeNavigation, flushVirtualizedList, renderWithProviders } from '../../test/render';
import { HomeScreen } from './HomeScreen';

jest.mock('../../api', () => {
  const actual = jest.requireActual('../../api');
  return {
    ...actual,
    fetchNewsPage: jest.fn(),
    fetchForumRecentThreads: jest.fn(),
    fetchPhotoCategories: jest.fn(),
    fetchOnThisDay: jest.fn(),
    fetchLiveActivity: jest.fn(),
    fetchInbox: jest.fn(),
  };
});

const mockSession = createMockSession();

jest.mock('../../session/SessionContext', () => ({
  useSession: () => mockSession,
}));

jest.mock('../messages/useUnreadConversationCount', () => ({
  useUnreadConversationCount: () => 0,
}));

jest.mock('expo-linear-gradient', () => {
  const { View } = require('react-native');
  return { LinearGradient: View };
});

const fetchNews = fetchNewsPage as jest.MockedFunction<typeof fetchNewsPage>;
const fetchForum = fetchForumRecentThreads as jest.MockedFunction<typeof fetchForumRecentThreads>;
const fetchPhotos = fetchPhotoCategories as jest.MockedFunction<typeof fetchPhotoCategories>;
const fetchDay = fetchOnThisDay as jest.MockedFunction<typeof fetchOnThisDay>;
const fetchLive = fetchLiveActivity as jest.MockedFunction<typeof fetchLiveActivity>;

function renderHome(navigation = fakeNavigation()) {
  return {
    navigation,
    ...renderWithProviders(
      <HomeScreen navigation={navigation as never} route={{ key: 'home', name: 'Home' } as never} />,
      { navigation: false },
    ),
  };
}

describe('HomeScreen', () => {
  beforeEach(() => {
    mockSession.isSignedIn = false;
    mockSession.accessToken = null;
    fetchNews.mockResolvedValue(
      pagedResponse(
        [
          newsItemFixture({ id: 1003, title: 'QueenZone modernisation begins' }),
          newsItemFixture({ id: 7, title: 'Live Aid remembered' }),
        ],
        1,
        1,
      ),
    );
    fetchForum.mockResolvedValue([
      {
        topicId: 1002,
        title: 'Ranking every studio album',
        categoryId: 1,
        categoryName: 'General',
        replyCount: 12,
        lastActivityAt: '2026-01-01T00:00:00.000Z',
        detailPath: '/forum/topic/1002/ranking-every-studio-album',
      },
    ]);
    fetchPhotos.mockResolvedValue(pagedResponse([], 1, 0));
    fetchDay.mockResolvedValue(null);
    fetchLive.mockResolvedValue({ newForumRepliesToday: 0 });
  });

  afterEach(async () => {
    await flushVirtualizedList();
  });

  it('opens live news and forum rows with numeric ids, not placeholders', async () => {
    const { navigation } = renderHome();
    await waitFor(() => expect(screen.getByRole('button', { name: 'Live Aid remembered' })).toBeOnTheScreen());
    await waitFor(() =>
      expect(screen.getByRole('button', { name: 'Ranking every studio album' })).toBeOnTheScreen(),
    );

    const user = userEvent.setup();
    await user.press(screen.getByRole('button', { name: 'Live Aid remembered' }));
    expect(navigation.navigate).toHaveBeenCalledWith('Story', { id: 7 });
    expect(navigation.navigate).not.toHaveBeenCalledWith(
      'ArchiveTab',
      expect.objectContaining({ params: { id: 0 } }),
    );

    await user.press(screen.getByRole('button', { name: 'Ranking every studio album' }));
    expect(navigation.navigate).toHaveBeenCalledWith('ForumTab', {
      screen: 'Thread',
      params: { id: 1002, title: 'Ranking every studio album' },
      initial: false,
    });
    expect(JSON.stringify(navigation.navigate.mock.calls)).not.toContain('magic-tour');
  });

  it('pull-to-refresh reloads live home sections', async () => {
    renderHome();
    await waitFor(() => expect(screen.getByRole('button', { name: 'Live Aid remembered' })).toBeOnTheScreen());
    const newsCalls = fetchNews.mock.calls.length;
    const forumCalls = fetchForum.mock.calls.length;
    await act(async () => {
      fireEvent(screen.UNSAFE_getByType(RefreshControl), 'refresh');
    });
    await waitFor(() => expect(fetchNews.mock.calls.length).toBeGreaterThan(newsCalls));
    expect(fetchForum.mock.calls.length).toBeGreaterThan(forumCalls);
    await flushVirtualizedList();
  });
});
