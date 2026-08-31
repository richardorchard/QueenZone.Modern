import type { ReactNode } from 'react';
import { FlatList } from 'react-native';
import { screen, userEvent, waitFor } from '@testing-library/react-native';
import { fetchTimelinePage } from '../../api';
import type { TimelineEvent } from '../../api/types';
import { pagedResponse } from '../../test/fixtures';
import { fakeNavigation, flushVirtualizedList, renderWithProviders } from '../../test/render';
import { testIds } from '../../test/testIds';
import { TimelineScreen } from './TimelineScreen';

jest.mock('../../api', () => {
  const actual = jest.requireActual('../../api');
  return {
    ...actual,
    fetchTimelinePage: jest.fn(),
  };
});

const fetchTimeline = fetchTimelinePage as jest.MockedFunction<typeof fetchTimelinePage>;

function eventFixture(overrides: Partial<TimelineEvent> = {}): TimelineEvent {
  return {
    id: 12,
    title: 'Live Aid',
    summary: 'Wembley Stadium.',
    eventDate: '1985-07-13T00:00:00Z',
    formattedDate: '13 July 1985',
    category: 'live',
    categoryLabel: 'Live',
    sourceUrl: null,
    ...overrides,
  };
}

describe('TimelineScreen', () => {
  beforeEach(() => {
    fetchTimeline.mockReset();
    fetchTimeline.mockResolvedValue(
      pagedResponse([eventFixture(), eventFixture({ id: 10, title: 'Another' })], 1, 1),
    );
    jest.spyOn(FlatList.prototype, 'scrollToIndex').mockImplementation(() => undefined);
  });

  afterEach(async () => {
    jest.restoreAllMocks();
    await flushVirtualizedList();
  });

  it('expands the event passed as focusId from search', async () => {
    renderWithProviders(
      <TimelineScreen
        navigation={fakeNavigation() as never}
        route={{ key: 'timeline', name: 'Timeline', params: { focusId: 12 } } as never}
      />,
      { navigation: false },
    );
    await waitFor(() => expect(screen.getByRole('button', { name: 'Live Aid' })).toBeOnTheScreen());
    expect(screen.getByRole('button', { name: 'Live Aid' }).props.accessibilityState).toEqual({
      expanded: true,
    });
    expect(screen.getByText('Wembley Stadium.')).toBeOnTheScreen();
    expect(screen.getByRole('button', { name: 'Another' }).props.accessibilityState).toEqual({
      expanded: false,
    });
    await waitFor(() =>
      expect(FlatList.prototype.scrollToIndex).toHaveBeenCalledWith(
        expect.objectContaining({ index: expect.any(Number) }),
      ),
    );
  });

  it('pages until the focused event appears, then expands that row', async () => {
    fetchTimeline.mockImplementation(async (query = {}) => {
      if ((query.page ?? 1) === 1) {
        return pagedResponse([eventFixture({ id: 10, title: 'Another' })], 1, 2);
      }
      return pagedResponse([eventFixture()], 2, 2);
    });

    renderWithProviders(
      <TimelineScreen
        navigation={fakeNavigation() as never}
        route={{ key: 'timeline', name: 'Timeline', params: { focusId: 12 } } as never}
      />,
      { navigation: false },
    );

    await waitFor(() => expect(fetchTimeline).toHaveBeenCalledTimes(2));
    await waitFor(() => expect(screen.getByRole('button', { name: 'Live Aid' })).toBeOnTheScreen());
    expect(screen.getByRole('button', { name: 'Live Aid' }).props.accessibilityState).toEqual({
      expanded: true,
    });
    expect(screen.getByText('Wembley Stadium.')).toBeOnTheScreen();
    expect(screen.getByRole('button', { name: 'Another' }).props.accessibilityState).toEqual({
      expanded: false,
    });
    await waitFor(() =>
      expect(FlatList.prototype.scrollToIndex).toHaveBeenCalledWith(
        expect.objectContaining({ index: expect.any(Number) }),
      ),
    );
  });

  it('leaves the list unexpanded when the focused event is never found', async () => {
    fetchTimeline.mockResolvedValue(pagedResponse([eventFixture({ id: 10, title: 'Another' })], 1, 1));

    renderWithProviders(
      <TimelineScreen
        navigation={fakeNavigation() as never}
        route={{ key: 'timeline', name: 'Timeline', params: { focusId: 99 } } as never}
      />,
      { navigation: false },
    );

    await waitFor(() => expect(screen.getByRole('button', { name: 'Another' })).toBeOnTheScreen());
    expect(screen.getByRole('button', { name: 'Another' }).props.accessibilityState).toEqual({
      expanded: false,
    });
    expect(screen.queryByText('Wembley Stadium.')).toBeNull();
    expect(FlatList.prototype.scrollToIndex).not.toHaveBeenCalled();
  });

  it('pops back to the archive listing when the stack has history', async () => {
    const navigation = fakeNavigation();
    navigation.canGoBack.mockReturnValue(true);
    renderTimeline(navigation);
    await waitFor(() => expect(screen.getByRole('button', { name: 'Live Aid' })).toBeOnTheScreen());

    const user = userEvent.setup();
    renderWithProviders(<>{lastHeaderLeft(navigation)()}</>, { navigation: false });
    await user.press(screen.getByTestId(testIds.timelineBack));
    expect(navigation.goBack).toHaveBeenCalledTimes(1);
    expect(navigation.navigate).not.toHaveBeenCalled();
  });

  it('falls back to ArchiveHub when Timeline is the only route', async () => {
    const navigation = fakeNavigation();
    navigation.canGoBack.mockReturnValue(false);
    renderTimeline(navigation);
    await waitFor(() => expect(screen.getByRole('button', { name: 'Live Aid' })).toBeOnTheScreen());

    const user = userEvent.setup();
    renderWithProviders(<>{lastHeaderLeft(navigation)()}</>, { navigation: false });
    await user.press(screen.getByTestId(testIds.timelineBack));
    expect(navigation.goBack).not.toHaveBeenCalled();
    expect(navigation.navigate).toHaveBeenCalledWith('ArchiveHub');
  });
});

function lastHeaderLeft(navigation: ReturnType<typeof fakeNavigation>) {
  const calls = navigation.setOptions.mock.calls;
  expect(calls.length).toBeGreaterThan(0);
  const options = calls[calls.length - 1]?.[0] as { headerLeft?: () => ReactNode };
  expect(options.headerLeft).toEqual(expect.any(Function));
  return options.headerLeft!;
}

function renderTimeline(navigation = fakeNavigation()) {
  return renderWithProviders(
    <TimelineScreen
      navigation={navigation as never}
      route={{ key: 'timeline', name: 'Timeline', params: { focusId: 12 } } as never}
    />,
    { navigation: false },
  );
}
