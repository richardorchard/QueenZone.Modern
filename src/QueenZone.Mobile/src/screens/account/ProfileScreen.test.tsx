import { screen, userEvent } from '@testing-library/react-native';
import { ProfileScreen } from './ProfileScreen';
import { memberProfileFixture } from '../../test/fixtures';
import { createMockSession } from '../../test/mockSession';
import { fakeNavigation, renderWithProviders } from '../../test/render';

const mockSession = createMockSession();

jest.mock('../../session/SessionContext', () => ({
  useSession: () => mockSession,
}));

jest.mock('../messages/useUnreadConversationCount', () => ({
  useUnreadConversationCount: () => 2,
}));

function renderProfile() {
  return renderWithProviders(
    <ProfileScreen navigation={fakeNavigation() as never} route={{ key: 'profile', name: 'Profile' } as never} />,
    { navigation: false },
  );
}

describe('ProfileScreen', () => {
  beforeEach(() => {
    mockSession.isSignedIn = false;
    mockSession.isRestoring = false;
    mockSession.displayName = null;
    mockSession.profile = null;
    mockSession.signOut.mockReset();
  });

  it('gates signed-out visitors behind Sign in', async () => {
    const navigation = fakeNavigation();
    const user = userEvent.setup();
    renderWithProviders(
      <ProfileScreen navigation={navigation as never} route={{ key: 'profile', name: 'Profile' } as never} />,
      { navigation: false },
    );
    expect(screen.getByText('Join the archive')).toBeOnTheScreen();
    await user.press(screen.getByRole('button', { name: 'Sign in' }));
    expect(navigation.dispatch).toHaveBeenCalledWith(
      expect.objectContaining({
        type: 'NAVIGATE',
        payload: expect.objectContaining({ name: 'SignIn' }),
      }),
    );
  });

  it('shows a restoring state instead of the signed-out gate', () => {
    mockSession.isRestoring = true;
    renderProfile();
    expect(screen.getByTestId('profile-restoring')).toBeOnTheScreen();
    expect(screen.queryByText('Join the archive')).toBeNull();
  });

  it('shows member identity and sign out when signed in', async () => {
    mockSession.isSignedIn = true;
    mockSession.displayName = 'Freddie';
    mockSession.profile = memberProfileFixture();
    mockSession.refreshProfile.mockResolvedValue(mockSession.profile);
    renderProfile();
    expect(screen.getByText('Freddie')).toBeOnTheScreen();
    expect(screen.getByRole('button', { name: 'Sign out' })).toBeOnTheScreen();
  });
});
