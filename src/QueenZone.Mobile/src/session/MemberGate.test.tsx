import { Text } from 'react-native';
import { screen, userEvent } from '@testing-library/react-native';
import { MemberGate } from './MemberGate';
import { createMockSession } from '../test/mockSession';
import { renderWithProviders } from '../test/render';

const mockSession = createMockSession();
const mockNavigate = jest.fn();

jest.mock('./SessionContext', () => ({
  useSession: () => mockSession,
}));

jest.mock('@react-navigation/native', () => {
  const actual = jest.requireActual('@react-navigation/native');
  return {
    ...actual,
    useNavigation: () => ({ navigate: mockNavigate }),
  };
});

describe('MemberGate', () => {
  beforeEach(() => {
    mockSession.isSignedIn = false;
    mockSession.isRestoring = false;
    mockNavigate.mockReset();
  });

  it('renders nothing while session restore is in flight', () => {
    mockSession.isRestoring = true;
    renderWithProviders(
      <MemberGate title="Messages">
        <Text>inbox</Text>
      </MemberGate>,
      { navigation: false },
    );
    expect(screen.queryByText('Messages')).toBeNull();
    expect(screen.queryByText('inbox')).toBeNull();
  });

  it('shows a sign-in gate and routes to HomeTab SignIn', async () => {
    const user = userEvent.setup();
    renderWithProviders(
      <MemberGate title="Messages">
        <Text>inbox</Text>
      </MemberGate>,
      { navigation: false },
    );
    expect(screen.getByText('Messages')).toBeOnTheScreen();
    expect(screen.getByText(/member-only boundary/)).toBeOnTheScreen();
    expect(screen.queryByText('inbox')).toBeNull();
    await user.press(screen.getByRole('button', { name: 'Sign in' }));
    expect(mockNavigate).toHaveBeenCalledWith('HomeTab', { screen: 'SignIn' });
  });

  it('renders children when signed in', () => {
    mockSession.isSignedIn = true;
    renderWithProviders(
      <MemberGate title="Messages">
        <Text>inbox</Text>
      </MemberGate>,
      { navigation: false },
    );
    expect(screen.getByText('inbox')).toBeOnTheScreen();
    expect(screen.queryByRole('button', { name: 'Sign in' })).toBeNull();
  });
});
