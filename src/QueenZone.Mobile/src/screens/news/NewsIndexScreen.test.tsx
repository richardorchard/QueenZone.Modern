import { screen, userEvent, waitFor } from '@testing-library/react-native';
import { fetchNewsPage } from '../../api';
import { ApiError } from '../../api/client';
import { deferred, newsItemFixture, pagedResponse } from '../../test/fixtures';
import { fakeNavigation, renderWithProviders } from '../../test/render';
import { NewsIndexScreen } from './NewsIndexScreen';

jest.mock('../../api', () => {
  const actual = jest.requireActual('../../api');
  return {
    ...actual,
    fetchNewsPage: jest.fn(),
  };
});

const fetchNews = fetchNewsPage as jest.MockedFunction<typeof fetchNewsPage>;

function renderNews(navigation = fakeNavigation()) {
  return {
    navigation,
    ...renderWithProviders(
      <NewsIndexScreen navigation={navigation as never} route={{ key: 'news', name: 'NewsIndex' } as never} />,
      { navigation: false },
    ),
  };
}

describe('NewsIndexScreen', () => {
  beforeEach(() => {
    fetchNews.mockReset();
  });

  it('shows loading then a labelled article that opens Story with the item id', async () => {
    const pending = deferred<ReturnType<typeof pagedResponse<ReturnType<typeof newsItemFixture>>>>();
    fetchNews.mockReturnValueOnce(pending.promise);
    const { navigation } = renderNews();
    expect(screen.getByLabelText('Loading news…')).toBeOnTheScreen();
    pending.resolve(pagedResponse([newsItemFixture({ id: 7, title: 'Live Aid' })], 1, 1));
    await waitFor(() => expect(screen.getByRole('button', { name: 'Open Live Aid' })).toBeOnTheScreen());
    const user = userEvent.setup();
    await user.press(screen.getByRole('button', { name: 'Open Live Aid' }));
    expect(navigation.navigate).toHaveBeenCalledWith('Story', { id: 7 });
  });

  it('shows empty copy when the list has no items', async () => {
    fetchNews.mockResolvedValue(pagedResponse([], 1, 0));
    renderNews();
    await waitFor(() => expect(screen.getByText('No news articles yet.')).toBeOnTheScreen());
  });

  it('shows an error and retries', async () => {
    fetchNews
      .mockRejectedValueOnce(new ApiError(0, 'Unable to reach QueenZone. Check your connection and try again.'))
      .mockResolvedValueOnce(pagedResponse([newsItemFixture()], 1, 1));
    renderNews();
    await waitFor(() => expect(screen.getByText('Unable to load')).toBeOnTheScreen());
    const user = userEvent.setup();
    await user.press(screen.getByRole('button', { name: 'Try again' }));
    await waitFor(() => expect(screen.getByRole('button', { name: 'Open Queen headline' })).toBeOnTheScreen());
  });

  it('picking a decade re-queries the server instead of filtering the loaded page', async () => {
    // Regression for #838: an older decade's first matching article can be well past whatever
    // page happens to be loaded, so filtering must be a fresh server request, not client-side.
    fetchNews.mockResolvedValueOnce(pagedResponse([newsItemFixture({ id: 1, title: 'Recent article' })], 1, 1));
    renderNews();
    await waitFor(() => expect(screen.getByRole('button', { name: 'Open Recent article' })).toBeOnTheScreen());
    expect(fetchNews).toHaveBeenCalledWith(expect.objectContaining({ page: 1, decade: undefined }));

    fetchNews.mockResolvedValueOnce(
      pagedResponse([newsItemFixture({ id: 9999, title: 'Old article from the 2000s' })], 1, 1),
    );
    const user = userEvent.setup();
    await user.press(screen.getByRole('button', { name: '2000s' }));

    await waitFor(() =>
      expect(screen.getByRole('button', { name: 'Open Old article from the 2000s' })).toBeOnTheScreen(),
    );
    expect(screen.queryByRole('button', { name: 'Open Recent article' })).not.toBeOnTheScreen();
    expect(fetchNews).toHaveBeenLastCalledWith(expect.objectContaining({ page: 1, decade: 2000 }));
  });

  it('the year rail jumps decade the same way the chips do', async () => {
    // #886: the rail is an additional way to reach the same server-side decade filter (#838).
    fetchNews.mockResolvedValueOnce(pagedResponse([newsItemFixture({ id: 1, title: 'Recent article' })], 1, 1));
    renderNews();
    await waitFor(() => expect(screen.getByRole('button', { name: 'Open Recent article' })).toBeOnTheScreen());

    fetchNews.mockResolvedValueOnce(
      pagedResponse([newsItemFixture({ id: 9999, title: 'Old article from the 2000s' })], 1, 1),
    );
    const user = userEvent.setup();
    await user.press(screen.getByRole('button', { name: 'Jump to 2000s' }));

    await waitFor(() =>
      expect(screen.getByRole('button', { name: 'Open Old article from the 2000s' })).toBeOnTheScreen(),
    );
    expect(fetchNews).toHaveBeenLastCalledWith(expect.objectContaining({ page: 1, decade: 2000 }));
  });

  it('shows decade-specific empty copy when the server returns no matches', async () => {
    fetchNews.mockResolvedValueOnce(pagedResponse([newsItemFixture()], 1, 1));
    renderNews();
    await waitFor(() => expect(screen.getByRole('button', { name: 'Open Queen headline' })).toBeOnTheScreen());

    fetchNews.mockResolvedValueOnce(pagedResponse([], 1, 0));
    const user = userEvent.setup();
    await user.press(screen.getByRole('button', { name: '2000s' }));

    await waitFor(() => expect(screen.getByText('No articles for this decade yet.')).toBeOnTheScreen());
  });
});
