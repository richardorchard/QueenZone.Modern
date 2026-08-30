import { screen, userEvent, waitFor } from '@testing-library/react-native';
import { fetchRandomTrivia } from '../../api';
import { ApiError } from '../../api/client';
import { deferred } from '../../test/fixtures';
import { renderWithProviders } from '../../test/render';
import { testIds } from '../../test/testIds';
import { TriviaScreen } from './TriviaScreen';

jest.mock('../../api', () => {
  const actual = jest.requireActual('../../api');
  return {
    ...actual,
    fetchRandomTrivia: jest.fn(),
  };
});

const fetchTrivia = fetchRandomTrivia as jest.MockedFunction<typeof fetchRandomTrivia>;

function renderTrivia() {
  return renderWithProviders(<TriviaScreen />, { navigation: false });
}

describe('TriviaScreen', () => {
  beforeEach(() => {
    fetchTrivia.mockReset();
  });

  it('shows a published fact, meta, and Next fact', async () => {
    fetchTrivia.mockResolvedValue({
      id: 12,
      text: 'The first Queen album was recorded in 1972.',
      category: 'Studio',
      difficulty: 'Easy',
      source: 'Queen archive',
    });
    renderTrivia();

    await waitFor(() => expect(screen.getByTestId(testIds.triviaScreen)).toBeOnTheScreen());
    expect(screen.getByText('The first Queen album was recorded in 1972.')).toBeOnTheScreen();
    expect(screen.getByTestId(testIds.triviaMeta)).toHaveTextContent('Studio · Easy · Queen archive');
    expect(screen.getByTestId(testIds.triviaNext)).toBeOnTheScreen();
  });

  it('omits the meta row when category, difficulty, and source are blank', async () => {
    fetchTrivia.mockResolvedValue({
      id: 3,
      text: 'Brian May built the Red Special.',
      category: '   ',
      difficulty: null,
      source: undefined,
    });
    renderTrivia();

    await waitFor(() => expect(screen.getByTestId(testIds.triviaScreen)).toBeOnTheScreen());
    expect(screen.getByText('Brian May built the Red Special.')).toBeOnTheScreen();
    expect(screen.queryByTestId(testIds.triviaMeta)).toBeNull();
  });

  it('fetches another fact when Next fact is pressed', async () => {
    const user = userEvent.setup();
    fetchTrivia
      .mockResolvedValueOnce({
        id: 1,
        text: 'First fact.',
      })
      .mockResolvedValueOnce({
        id: 2,
        text: 'Second fact.',
        category: 'Live',
      });
    renderTrivia();

    await waitFor(() => expect(screen.getByText('First fact.')).toBeOnTheScreen());
    await user.press(screen.getByTestId(testIds.triviaNext));
    await waitFor(() => expect(screen.getByText('Second fact.')).toBeOnTheScreen());
    expect(screen.getByTestId(testIds.triviaMeta)).toHaveTextContent('Live');
    expect(fetchTrivia).toHaveBeenCalledTimes(2);
  });

  it('shows an empty state when the API returns null', async () => {
    fetchTrivia.mockResolvedValue(null);
    renderTrivia();

    await waitFor(() => expect(screen.getByTestId(testIds.triviaScreen)).toBeOnTheScreen());
    expect(screen.getByText('No trivia facts have been published yet.')).toBeOnTheScreen();
    expect(screen.queryByTestId(testIds.triviaNext)).toBeNull();
  });

  it('shows an error with retry when the request fails', async () => {
    const user = userEvent.setup();
    const pending = deferred<null>();
    fetchTrivia.mockRejectedValueOnce(new ApiError(500, 'The server had a problem. Try again shortly.'));
    fetchTrivia.mockReturnValueOnce(pending.promise);
    renderTrivia();

    await waitFor(() => expect(screen.getByText('Unable to load')).toBeOnTheScreen());
    expect(screen.getByText('The server had a problem. Try again shortly.')).toBeOnTheScreen();

    await user.press(screen.getByRole('button', { name: 'Try again' }));
    pending.resolve(null);
    await waitFor(() => expect(screen.getByTestId(testIds.triviaScreen)).toBeOnTheScreen());
    expect(screen.getByText('No trivia facts have been published yet.')).toBeOnTheScreen();
  });
});
