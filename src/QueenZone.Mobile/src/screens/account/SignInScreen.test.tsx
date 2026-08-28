import { screen, userEvent, waitFor } from '@testing-library/react-native';
import { fallbackAuthProviders } from '../../api/auth';
import { jsonResponse } from '../../test/fixtures';
import { createMockSession } from '../../test/mockSession';
import { fakeNavigation, renderWithProviders } from '../../test/render';
import { SignInScreen } from './SignInScreen';

const mockSession = createMockSession();

jest.mock('../../session/SessionContext', () => ({
  useSession: () => mockSession,
}));

const fetchMock = jest.fn<Promise<Response>, [RequestInfo | URL, RequestInit?]>();

beforeEach(() => {
  mockSession.isSignedIn = false;
  mockSession.signIn.mockReset();
  fetchMock.mockReset();
  global.fetch = fetchMock as unknown as typeof fetch;
  fetchMock.mockResolvedValue(jsonResponse({ providers: fallbackAuthProviders }));
});

function renderSignIn(returnTo?: { tab: 'ForumTab'; screen: 'Composer'; params: { threadId: number } }) {
  const navigation = fakeNavigation();
  renderWithProviders(
    <SignInScreen
      navigation={navigation as never}
      route={{ key: 'signin', name: 'SignIn', params: returnTo ? { returnTo } : undefined } as never}
    />,
    { navigation: false },
  );
  return navigation;
}

describe('SignInScreen', () => {
  it('lists providers and surfaces a sign-in failure', async () => {
    const user = userEvent.setup();
    mockSession.signIn.mockRejectedValue(new Error('Sign-in was cancelled.'));
    renderSignIn();
    await waitFor(() => expect(screen.getByRole('button', { name: 'Continue with Google' })).toBeOnTheScreen());
    await user.press(screen.getByRole('button', { name: 'Continue with Google' }));
    await waitFor(() => expect(screen.getByRole('alert')).toHaveTextContent('Sign-in was cancelled.'));
  });

  it('uses the Apple-approved visual treatment for the Apple provider', async () => {
    renderSignIn();
    await waitFor(() => expect(screen.getByTestId('apple-sign-in-button')).toBeOnTheScreen());
    expect(screen.getByRole('button', { name: 'Continue with Apple' })).toBeOnTheScreen();
  });

  it('leaves for the prompting screen after a successful provider hop', async () => {
    const user = userEvent.setup();
    mockSession.signIn.mockResolvedValue(undefined);
    const navigation = renderSignIn({ tab: 'ForumTab', screen: 'Composer', params: { threadId: 9 } });
    await waitFor(() => expect(screen.getByRole('button', { name: 'Continue with Google' })).toBeOnTheScreen());
    await user.press(screen.getByRole('button', { name: 'Continue with Google' }));
    await waitFor(() =>
      expect(navigation.navigate).toHaveBeenCalledWith('Tabs', {
        screen: 'ForumTab',
        params: { screen: 'Composer', params: { threadId: 9 } },
      }),
    );
  });
});
