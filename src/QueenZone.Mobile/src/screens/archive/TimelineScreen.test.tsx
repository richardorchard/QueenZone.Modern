import { screen, waitFor } from '@testing-library/react-native';
import { fetchTimelinePage } from '../../api';
import type { TimelineEvent } from '../../api/types';
import { pagedResponse } from '../../test/fixtures';
import { fakeNavigation, renderWithProviders } from '../../test/render';
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
  });
});
