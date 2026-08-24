import { screen, userEvent, waitFor } from '@testing-library/react-native';
import { fallbackAuthProviders } from '../../api/auth';
import { jsonResponse } from '../../test/fixtures';
import { createMockSession } from '../../test/mockSession';
import { renderWithProviders } from '../../test/render';
import { SignInScreen } from './SignInScreen';

const mockSession = createMockSession();

jest.mock('../../session/SessionContext', () => ({
  useSession: () => mockSession,
}));

const fetchMock = jest.fn<Promise<Response>, [RequestInfo | URL, RequestInit?]>();

beforeEach(() => {
  mockSession.signIn.mockReset();
  fetchMock.mockReset();
  global.fetch = fetchMock as unknown as typeof fetch;
  fetchMock.mockResolvedValue(jsonResponse({ providers: fallbackAuthProviders }));
});

describe('SignInScreen', () => {
  it('lists providers and surfaces a sign-in failure', async () => {
    const user = userEvent.setup();
    mockSession.signIn.mockRejectedValue(new Error('Sign-in was cancelled.'));
    renderWithProviders(<SignInScreen />, { navigation: false });
    await waitFor(() => expect(screen.getByRole('button', { name: 'Continue with Google' })).toBeOnTheScreen());
    await user.press(screen.getByRole('button', { name: 'Continue with Google' }));
    await waitFor(() => expect(screen.getByRole('alert')).toHaveTextContent('Sign-in was cancelled.'));
  });
});
