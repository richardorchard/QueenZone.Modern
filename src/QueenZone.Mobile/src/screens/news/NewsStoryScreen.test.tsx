import type { ReactNode } from 'react';
import { screen, userEvent, waitFor } from '@testing-library/react-native';
import { fetchNewsDetail } from '../../api';
import { ApiError } from '../../api/client';
import { newsDetailFixture } from '../../test/fixtures';
import { fakeNavigation, renderWithProviders } from '../../test/render';
import { testIds } from '../../test/testIds';
import { NewsStoryScreen } from './NewsStoryScreen';

jest.mock('../../api', () => {
  const actual = jest.requireActual('../../api');
  return {
    ...actual,
    fetchNewsDetail: jest.fn(),
  };
});

const fetchDetail = fetchNewsDetail as jest.MockedFunction<typeof fetchNewsDetail>;

function lastHeaderOptions(navigation: ReturnType<typeof fakeNavigation>) {
  const calls = navigation.setOptions.mock.calls;
  expect(calls.length).toBeGreaterThan(0);
  return calls[calls.length - 1]?.[0] as { headerLeft?: () => ReactNode; title?: string };
}

function renderStory(navigation = fakeNavigation(), id = 42) {
  return {
    navigation,
    ...renderWithProviders(
      <NewsStoryScreen
        navigation={navigation as never}
        route={{ key: 'story', name: 'Story', params: { id } } as never}
      />,
      { navigation: false },
    ),
  };
}

describe('NewsStoryScreen', () => {
  beforeEach(() => {
    fetchDetail.mockReset();
    fetchDetail.mockResolvedValue(
      newsDetailFixture({
        id: 7,
        title: "Roger Taylor Releases New Single and Video 'I See You Now'",
      }),
    );
  });

  it('installs a back control that returns to NewsIndex when the stack has no history', async () => {
    const navigation = fakeNavigation();
    navigation.canGoBack.mockReturnValue(false);
    renderStory(navigation, 7);
    await waitFor(() => expect(screen.getByTestId(testIds.newsStoryScreen)).toBeOnTheScreen());

    const options = lastHeaderOptions(navigation);
    expect(options.title).toBe("Roger Taylor Releases New Single and Video 'I See You Now'");
    expect(options.headerLeft).toEqual(expect.any(Function));

    const user = userEvent.setup();
    renderWithProviders(<>{options.headerLeft?.()}</>, { navigation: false });
    await user.press(screen.getByTestId(testIds.newsStoryBack));
    expect(navigation.goBack).not.toHaveBeenCalled();
    expect(navigation.navigate).toHaveBeenCalledWith('NewsIndex');
  });

  it('pops the news stack when back has somewhere to go', async () => {
    const navigation = fakeNavigation();
    navigation.canGoBack.mockReturnValue(true);
    renderStory(navigation, 7);
    await waitFor(() => expect(screen.getByTestId(testIds.newsStoryScreen)).toBeOnTheScreen());

    const user = userEvent.setup();
    renderWithProviders(<>{lastHeaderOptions(navigation).headerLeft?.()}</>, { navigation: false });
    await user.press(screen.getByLabelText('Back'));
    expect(navigation.goBack).toHaveBeenCalledTimes(1);
    expect(navigation.navigate).not.toHaveBeenCalled();
  });

  it('shows an error and retries', async () => {
    fetchDetail
      .mockRejectedValueOnce(new ApiError(0, 'Unable to reach QueenZone. Check your connection and try again.'))
      .mockResolvedValueOnce(newsDetailFixture({ title: 'Retried headline' }));
    renderStory();
    await waitFor(() => expect(screen.getByText('Unable to load')).toBeOnTheScreen());
    const user = userEvent.setup();
    await user.press(screen.getByRole('button', { name: 'Try again' }));
    await waitFor(() => expect(screen.getByText('Retried headline')).toBeOnTheScreen());
  });
});
