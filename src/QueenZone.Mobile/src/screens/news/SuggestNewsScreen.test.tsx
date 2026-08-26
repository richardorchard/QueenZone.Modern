import { screen, userEvent, waitFor } from '@testing-library/react-native';
import type { NewsShareView } from '../../share/news/session';
import { createMockSession } from '../../test/mockSession';
import { fakeNavigation, renderWithProviders } from '../../test/render';
import { testIds } from '../../test/testIds';
import { SuggestNewsScreen } from './SuggestNewsScreen';

const mockSession = createMockSession();
const mockFlush = jest.fn(async () => undefined);
let mockShare: NewsShareView = { kind: 'idle' };

jest.mock('../../session/SessionContext', () => ({
  useSession: () => mockSession,
}));

jest.mock('../../share/news/NewsShare', () => ({
  useNewsShare: () => mockShare,
  getNewsShareController: () => ({ flush: mockFlush }),
  openSuggestNews: jest.fn(),
}));

function renderSuggest(navigation = fakeNavigation()) {
  return {
    navigation,
    ...renderWithProviders(
      <SuggestNewsScreen
        navigation={navigation as never}
        route={{ key: 'suggest', name: 'SuggestNews' } as never}
      />,
      { navigation: false },
    ),
  };
}

describe('SuggestNewsScreen', () => {
  beforeEach(() => {
    mockSession.isSignedIn = false;
    mockSession.accessToken = null;
    mockFlush.mockClear();
    mockShare = {
      kind: 'form',
      draft: {
        url: 'https://www.bbc.co.uk/news/example',
        title: 'Queen announce dates',
        notes: '',
        origin: 'share',
      },
      patch: jest.fn(),
      cancel: jest.fn(),
      submit: jest.fn(),
    };
  });

  it('shows a signed-out form and signs in with SuggestNews returnTo', async () => {
    const { navigation } = renderSuggest();
    expect(screen.getByTestId(testIds.suggestNewsScreen)).toBeOnTheScreen();
    expect(screen.getByTestId(testIds.suggestNewsUrl)).toBeOnTheScreen();
    expect(screen.getByDisplayValue('https://www.bbc.co.uk/news/example')).toBeOnTheScreen();
    expect(screen.getByText('www.bbc.co.uk')).toBeOnTheScreen();
    expect(screen.getByTestId(testIds.suggestNewsSubmit)).toBeDisabled();

    const user = userEvent.setup();
    await user.press(screen.getByTestId(testIds.suggestNewsSignIn));
    await waitFor(() => expect(mockFlush).toHaveBeenCalled());
    expect(navigation.dispatch).toHaveBeenCalledWith(
      expect.objectContaining({
        type: 'NAVIGATE',
        payload: expect.objectContaining({
          name: 'SignIn',
          params: { returnTo: { tab: 'HomeTab', screen: 'SuggestNews' } },
        }),
      }),
    );
  });

  it('lets the member pick from the chooser', async () => {
    const choose = jest.fn();
    mockShare = {
      kind: 'choose',
      candidates: ['https://www.bbc.co.uk/one', 'https://www.bbc.co.uk/two'],
      choose,
      cancel: jest.fn(),
    };
    renderSuggest();
    expect(screen.getByTestId(testIds.suggestNewsChooser)).toBeOnTheScreen();
    const user = userEvent.setup();
    await user.press(screen.getByLabelText('Use https://www.bbc.co.uk/two'));
    expect(choose).toHaveBeenCalledWith('https://www.bbc.co.uk/two');
  });

  it('routes success to My submissions', async () => {
    const acknowledge = jest.fn();
    mockShare = {
      kind: 'submitted',
      created: {
        id: '11111111-1111-1111-1111-111111111111',
        status: 'Pending',
        url: 'https://www.bbc.co.uk/news/example',
        title: 'Queen announce dates',
        submittedAt: '2026-08-26T10:00:00Z',
      },
      acknowledge,
    };
    const { navigation } = renderSuggest();
    expect(screen.getByTestId(testIds.suggestNewsSuccess)).toBeOnTheScreen();
    const user = userEvent.setup();
    await user.press(screen.getByRole('button', { name: 'View my submissions' }));
    expect(acknowledge).toHaveBeenCalled();
    expect(navigation.navigate).toHaveBeenCalledWith('MySubmissions');
  });

  it('retries a failed network submit', async () => {
    const submit = jest.fn();
    mockSession.isSignedIn = true;
    mockSession.accessToken = 'tok';
    mockShare = {
      kind: 'failed',
      draft: {
        url: 'https://www.bbc.co.uk/news/example',
        title: '',
        notes: '',
        origin: 'share',
      },
      error: {
        code: 'network',
        message: 'Unable to reach QueenZone. Check your connection and try again.',
        retryable: true,
      },
      patch: jest.fn(),
      cancel: jest.fn(),
      submit,
    };
    renderSuggest();
    const user = userEvent.setup();
    await user.press(screen.getByTestId(testIds.suggestNewsRetry));
    expect(submit).toHaveBeenCalledWith('tok');
  });
});
