import { screen, userEvent, waitFor } from '@testing-library/react-native';
import * as WebBrowser from 'expo-web-browser';
import { fetchSearchPage } from '../../api';
import { ApiError } from '../../api/client';
import type { SearchResult } from '../../api/types';
import { deferred, pagedResponse } from '../../test/fixtures';
import { renderWithProviders } from '../../test/render';
import { SearchScreen } from './SearchScreen';

jest.mock('../../api', () => {
  const actual = jest.requireActual('../../api');
  return {
    ...actual,
    fetchSearchPage: jest.fn(),
  };
});

jest.mock('../../config/appConfig', () => ({
  getAppConfig: () => ({ apiBaseUrl: 'https://www.queenzone.org', appEnv: 'development', version: '0.1.0' }),
}));

const fetchSearch = fetchSearchPage as jest.MockedFunction<typeof fetchSearchPage>;

function resultFixture(overrides: Partial<SearchResult> = {}): SearchResult {
  return {
    contentType: 'news',
    sourceKey: 'news:1003',
    title: 'QueenZone modernisation begins',
    summary: 'The rebuild starts.',
    url: '/news/1003/queenzone-modernisation-begins',
    publishedAt: '2026-06-11T09:00:00Z',
    imageUrl: null,
    category: null,
    authorDisplayName: null,
    id: 1003,
    ...overrides,
  };
}

function renderSearch(onOpen = jest.fn()) {
  return {
    onOpen,
    ...renderWithProviders(<SearchScreen onOpen={onOpen} />, { navigation: false }),
  };
}

describe('SearchScreen', () => {
  beforeEach(() => {
    fetchSearch.mockReset();
  });

  it('shows suggested query presets and does not search one character', async () => {
    renderSearch();
    expect(screen.getByRole('button', { name: 'Search for Bohemian Rhapsody' })).toBeOnTheScreen();
    const user = userEvent.setup();
    await user.type(screen.getByLabelText('Search the archive'), 'a');
    await waitFor(() => expect(fetchSearch).not.toHaveBeenCalled());
  });

  it('loads live results and opens a news story by numeric id', async () => {
    const pending = deferred<ReturnType<typeof pagedResponse<SearchResult>>>();
    fetchSearch.mockReturnValueOnce(pending.promise);
    const { onOpen } = renderSearch();
    const user = userEvent.setup();
    await user.type(screen.getByLabelText('Search the archive'), 'modernisation');
    await waitFor(() => expect(screen.getByLabelText('Searching the archive…')).toBeOnTheScreen());
    pending.resolve(pagedResponse([resultFixture()], 1, 1));
    await waitFor(() =>
      expect(screen.getByRole('button', { name: 'QueenZone modernisation begins. News' })).toBeOnTheScreen(),
    );
    expect(fetchSearch).toHaveBeenCalledWith(
      expect.objectContaining({ q: 'modernisation', type: null, page: 1, pageSize: 20 }),
    );
    await user.press(screen.getByRole('button', { name: 'QueenZone modernisation begins. News' }));
    expect(onOpen).toHaveBeenCalledWith(
      { kind: 'tab', tab: 'NewsTab', screen: 'Story', params: { id: 1003 } },
      expect.objectContaining({ sourceKey: 'news:1003', id: 1003 }),
    );
    expect(onOpen.mock.calls[0][0]).not.toMatchObject({ params: { id: 0 } });
  });

  it('opens a forum thread by numeric topic id, not a slug', async () => {
    fetchSearch.mockResolvedValue(
      pagedResponse(
        [
          resultFixture({
            contentType: 'forum',
            sourceKey: 'forum-thread:1002',
            title: 'Ranking every studio album',
            url: '/forum/topic/1002/ranking-every-studio-album',
            id: 1002,
          }),
        ],
        1,
        1,
      ),
    );
    const { onOpen } = renderSearch();
    const user = userEvent.setup();
    await user.type(screen.getByLabelText('Search the archive'), 'studio album');
    await waitFor(() =>
      expect(screen.getByRole('button', { name: 'Ranking every studio album. Forum' })).toBeOnTheScreen(),
    );
    await user.press(screen.getByRole('button', { name: 'Ranking every studio album. Forum' }));
    expect(onOpen).toHaveBeenCalledWith(
      { kind: 'tab', tab: 'ForumTab', screen: 'Thread', params: { id: 1002 } },
      expect.objectContaining({ id: 1002 }),
    );
    expect(JSON.stringify(onOpen.mock.calls[0][0])).not.toContain('magic-tour');
  });

  it('shows empty copy when the index has no matches', async () => {
    fetchSearch.mockResolvedValue(pagedResponse([], 1, 0));
    renderSearch();
    const user = userEvent.setup();
    await user.type(screen.getByLabelText('Search the archive'), 'xyzzy unmatched');
    await waitFor(() =>
      expect(screen.getByText('No results found for “xyzzy unmatched”. Try different keywords.')).toBeOnTheScreen(),
    );
  });

  it('shows an error and retries', async () => {
    fetchSearch
      .mockRejectedValueOnce(new ApiError(0, 'Unable to reach QueenZone. Check your connection and try again.'))
      .mockResolvedValueOnce(pagedResponse([resultFixture()], 1, 1));
    renderSearch();
    const user = userEvent.setup();
    await user.type(screen.getByLabelText('Search the archive'), 'modernisation');
    await waitFor(() => expect(screen.getByText('Unable to load')).toBeOnTheScreen());
    await user.press(screen.getByRole('button', { name: 'Try again' }));
    await waitFor(() =>
      expect(screen.getByRole('button', { name: 'QueenZone modernisation begins. News' })).toBeOnTheScreen(),
    );
  });

  it('narrows the live query when a type chip is pressed', async () => {
    fetchSearch.mockResolvedValue(pagedResponse([resultFixture()], 1, 1));
    renderSearch();
    const user = userEvent.setup();
    await user.type(screen.getByLabelText('Search the archive'), 'archive');
    await waitFor(() => expect(screen.getByText('News')).toBeOnTheScreen());
    await user.press(screen.getByText('News'));
    await waitFor(() =>
      expect(fetchSearch).toHaveBeenCalledWith(expect.objectContaining({ q: 'archive', type: 'news' })),
    );
  });
});

describe('SearchRouteScreen', () => {
  it('opens article hits in the in-app browser', async () => {
    const openBrowser = WebBrowser.openBrowserAsync as jest.MockedFunction<typeof WebBrowser.openBrowserAsync>;
    openBrowser.mockClear();
    fetchSearch.mockResolvedValue(
      pagedResponse(
        [
          resultFixture({
            contentType: 'article',
            sourceKey: 'article:some-slug',
            title: 'A community article',
            url: '/articles/some-slug',
            id: null,
          }),
        ],
        1,
        1,
      ),
    );
    const onOpen = (target: { kind: string; url?: string }) => {
      if (target.kind === 'web' && target.url) {
        void WebBrowser.openBrowserAsync(target.url);
      }
    };
    renderWithProviders(<SearchScreen onOpen={onOpen as never} />, { navigation: false });
    const user = userEvent.setup();
    await user.type(screen.getByLabelText('Search the archive'), 'community');
    await waitFor(() =>
      expect(screen.getByRole('button', { name: 'A community article. Articles' })).toBeOnTheScreen(),
    );
    await user.press(screen.getByRole('button', { name: 'A community article. Articles' }));
    expect(openBrowser).toHaveBeenCalledWith('https://www.queenzone.org/articles/some-slug');
  });
});
