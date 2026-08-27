import { act, fireEvent, screen, userEvent, waitFor } from '@testing-library/react-native';
import { RefreshControl } from 'react-native';
import {
  fetchForumRecentThreads,
  fetchInbox,
  fetchLiveActivity,
  fetchNewsPage,
  fetchOnThisDay,
  fetchPhotoCategories,
  fetchRandomQuote,
} from '../../api';
import { deferred, newsItemFixture, pagedResponse } from '../../test/fixtures';
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
    fetchRandomQuote: jest.fn(),
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

const mockSyncHomeWidget = jest.fn().mockResolvedValue(undefined);
jest.mock('../../widgets/widgetSync', () => ({
  syncHomeWidget: (...args: unknown[]) => mockSyncHomeWidget(...args),
}));

const fetchNews = fetchNewsPage as jest.MockedFunction<typeof fetchNewsPage>;
const fetchForum = fetchForumRecentThreads as jest.MockedFunction<typeof fetchForumRecentThreads>;
const fetchPhotos = fetchPhotoCategories as jest.MockedFunction<typeof fetchPhotoCategories>;
const fetchDay = fetchOnThisDay as jest.MockedFunction<typeof fetchOnThisDay>;
const fetchQuote = fetchRandomQuote as jest.MockedFunction<typeof fetchRandomQuote>;
const fetchLive = fetchLiveActivity as jest.MockedFunction<typeof fetchLiveActivity>;
const fetchInboxMock = fetchInbox as jest.MockedFunction<typeof fetchInbox>;

function onThisDayFixture() {
  return {
    id: 1,
    title: 'The Game',
    summary: 'Queen released The Game.',
    eventDate: '1980-06-30',
    formattedDate: '30 June 1980',
    category: 'music',
    categoryLabel: 'Release',
    sourceUrl: null,
  };
}

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
    fetchQuote.mockResolvedValue(null);
    fetchLive.mockResolvedValue({ newForumRepliesToday: 0 });
    mockSyncHomeWidget.mockClear();
    fetchInboxMock.mockReset();
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

  it('holds RefreshControl refreshing until the deferred news fetch settles and refetches every section', async () => {
    renderHome();
    await waitFor(() => expect(screen.getByRole('button', { name: 'Live Aid remembered' })).toBeOnTheScreen());

    const newsCalls = fetchNews.mock.calls.length;
    const forumCalls = fetchForum.mock.calls.length;
    const photoCalls = fetchPhotos.mock.calls.length;
    const dayCalls = fetchDay.mock.calls.length;
    const quoteCalls = fetchQuote.mock.calls.length;
    const liveCalls = fetchLive.mock.calls.length;
    const inboxCalls = fetchInboxMock.mock.calls.length;

    const pendingNews = deferred<ReturnType<typeof pagedResponse<ReturnType<typeof newsItemFixture>>>>();
    fetchNews.mockReturnValueOnce(pendingNews.promise);

    await act(async () => {
      fireEvent(screen.UNSAFE_getByType(RefreshControl), 'refresh');
    });

    expect(screen.UNSAFE_getByType(RefreshControl).props.refreshing).toBe(true);
    await waitFor(() => expect(fetchNews.mock.calls.length).toBe(newsCalls + 1));
    expect(fetchForum.mock.calls.length).toBe(forumCalls + 1);
    expect(fetchPhotos.mock.calls.length).toBe(photoCalls + 1);
    expect(fetchDay.mock.calls.length).toBe(dayCalls + 1);
    expect(fetchQuote.mock.calls.length).toBe(quoteCalls + 1);
    expect(fetchLive.mock.calls.length).toBe(liveCalls + 1);
    expect(fetchInboxMock.mock.calls.length).toBe(inboxCalls);

    pendingNews.resolve(
      pagedResponse(
        [
          newsItemFixture({ id: 1003, title: 'QueenZone modernisation begins' }),
          newsItemFixture({ id: 7, title: 'Live Aid remembered' }),
        ],
        1,
        1,
      ),
    );
    await waitFor(() => expect(screen.UNSAFE_getByType(RefreshControl).props.refreshing).toBe(false));
    await flushVirtualizedList();
  });

  it('keeps the live strip visible while a pull refreshes', async () => {
    fetchLive.mockResolvedValue({ newForumRepliesToday: 4 });
    renderHome();
    await waitFor(() => expect(screen.getByText('4 new forum replies today')).toBeOnTheScreen());

    const pendingNews = deferred<ReturnType<typeof pagedResponse<ReturnType<typeof newsItemFixture>>>>();
    fetchNews.mockReturnValueOnce(pendingNews.promise);
    await act(async () => {
      fireEvent(screen.UNSAFE_getByType(RefreshControl), 'refresh');
    });

    expect(screen.UNSAFE_getByType(RefreshControl).props.refreshing).toBe(true);
    expect(screen.getByText('4 new forum replies today')).toBeOnTheScreen();

    pendingNews.resolve(
      pagedResponse(
        [
          newsItemFixture({ id: 1003, title: 'QueenZone modernisation begins' }),
          newsItemFixture({ id: 7, title: 'Live Aid remembered' }),
        ],
        1,
        1,
      ),
    );
    await waitFor(() => expect(screen.UNSAFE_getByType(RefreshControl).props.refreshing).toBe(false));
    expect(screen.getByText('4 new forum replies today')).toBeOnTheScreen();
    await flushVirtualizedList();
  });

  it('refetches filter-hidden sections on pull', async () => {
    renderHome();
    await waitFor(() => expect(screen.getByRole('button', { name: 'Live Aid remembered' })).toBeOnTheScreen());

    const user = userEvent.setup();
    await user.press(screen.getByRole('button', { name: 'News' }));
    await waitFor(() =>
      expect(screen.queryByRole('button', { name: 'Ranking every studio album' })).not.toBeOnTheScreen(),
    );

    const forumCalls = fetchForum.mock.calls.length;
    const dayCalls = fetchDay.mock.calls.length;
    await act(async () => {
      fireEvent(screen.UNSAFE_getByType(RefreshControl), 'refresh');
    });
    expect(fetchForum.mock.calls.length).toBe(forumCalls + 1);
    expect(fetchDay.mock.calls.length).toBe(dayCalls + 1);
    await flushVirtualizedList();
  });

  it('keeps On This Day visible while a pull refreshes', async () => {
    fetchDay.mockResolvedValue(onThisDayFixture());
    renderHome();
    await waitFor(() => expect(screen.getByText('Queen released The Game.')).toBeOnTheScreen());

    const pendingNews = deferred<ReturnType<typeof pagedResponse<ReturnType<typeof newsItemFixture>>>>();
    fetchNews.mockReturnValueOnce(pendingNews.promise);
    await act(async () => {
      fireEvent(screen.UNSAFE_getByType(RefreshControl), 'refresh');
    });

    expect(screen.UNSAFE_getByType(RefreshControl).props.refreshing).toBe(true);
    expect(screen.getByText('Queen released The Game.')).toBeOnTheScreen();
    expect(screen.getByText('30 JUNE 1980')).toBeOnTheScreen();

    pendingNews.resolve(
      pagedResponse(
        [
          newsItemFixture({ id: 1003, title: 'QueenZone modernisation begins' }),
          newsItemFixture({ id: 7, title: 'Live Aid remembered' }),
        ],
        1,
        1,
      ),
    );
    await waitFor(() => expect(screen.UNSAFE_getByType(RefreshControl).props.refreshing).toBe(false));
    expect(screen.getByText('Queen released The Game.')).toBeOnTheScreen();
    await flushVirtualizedList();
  });

  it('shows the quote of the day inside the On This Day card', async () => {
    fetchDay.mockResolvedValue(onThisDayFixture());
    fetchQuote.mockResolvedValue({ id: 9, text: 'A kind of magic', whoSaid: 'Freddie Mercury' });
    renderHome();

    await waitFor(() => expect(screen.getByText('Queen released The Game.')).toBeOnTheScreen());
    expect(screen.getByText('“A kind of magic”')).toBeOnTheScreen();
    expect(screen.getByText('— Freddie Mercury')).toBeOnTheScreen();
    await flushVirtualizedList();
  });

  it('omits the quote row when no quote is published', async () => {
    fetchDay.mockResolvedValue(onThisDayFixture());
    fetchQuote.mockResolvedValue(null);
    renderHome();

    await waitFor(() => expect(screen.getByText('Queen released The Game.')).toBeOnTheScreen());
    expect(screen.queryByText(/^“/)).not.toBeOnTheScreen();
    await flushVirtualizedList();
  });

  it('syncs the home screen widget once on-this-day content resolves', async () => {
    fetchDay.mockResolvedValue(onThisDayFixture());
    fetchQuote.mockResolvedValue({ id: 9, text: 'A kind of magic', whoSaid: 'Freddie Mercury' });
    renderHome();

    await waitFor(() =>
      expect(mockSyncHomeWidget).toHaveBeenCalledWith({
        onThisDay: onThisDayFixture(),
        quote: { id: 9, text: 'A kind of magic', whoSaid: 'Freddie Mercury' },
      }),
    );
    await flushVirtualizedList();
  });

  it('syncs a quote-only widget when on-this-day fails', async () => {
    fetchDay.mockRejectedValue(new Error('offline'));
    fetchQuote.mockResolvedValue({ id: 9, text: 'A kind of magic', whoSaid: 'Freddie Mercury' });
    renderHome();

    await waitFor(() =>
      expect(mockSyncHomeWidget).toHaveBeenCalledWith({
        onThisDay: null,
        quote: { id: 9, text: 'A kind of magic', whoSaid: 'Freddie Mercury' },
      }),
    );
    expect(screen.getByText('“A kind of magic”')).toBeOnTheScreen();
    expect(screen.getByText('Quote')).toBeOnTheScreen();
    await flushVirtualizedList();
  });

  it('shows the quote card when there is no on-this-day event', async () => {
    fetchDay.mockResolvedValue(null);
    fetchQuote.mockResolvedValue({ id: 9, text: 'A kind of magic', whoSaid: 'Freddie Mercury' });
    renderHome();

    await waitFor(() => expect(screen.getByText('“A kind of magic”')).toBeOnTheScreen());
    expect(screen.getByText('— Freddie Mercury')).toBeOnTheScreen();
    expect(screen.getByText('Quote')).toBeOnTheScreen();
    expect(screen.queryByText('Queen released The Game.')).not.toBeOnTheScreen();
    expect(screen.getByRole('button', { name: 'View timeline' })).toBeOnTheScreen();
    await flushVirtualizedList();
  });
});
