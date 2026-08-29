import { screen, userEvent } from '@testing-library/react-native';
import { createMockSession } from '../../test/mockSession';
import { renderWithProviders } from '../../test/render';
import { testIds } from '../../test/testIds';
import { TabRootMasthead } from './TabRootMasthead';

const mockSession = createMockSession();
const mockUnread = { count: 0 };

jest.mock('../../session/SessionContext', () => ({
  useSession: () => mockSession,
}));

jest.mock('../messages/useUnreadConversationCount', () => ({
  useUnreadConversationCount: () => mockUnread.count,
}));

function renderMasthead(
  props: { onSearch?: () => void; onProfilePress?: () => void; onMessagesPress?: () => void } = {},
) {
  const onSearch = props.onSearch ?? jest.fn();
  const onProfilePress = props.onProfilePress ?? jest.fn();
  const onMessagesPress = props.onMessagesPress ?? jest.fn();
  return {
    onSearch,
    onProfilePress,
    onMessagesPress,
    ...renderWithProviders(
      <TabRootMasthead
        onSearch={onSearch}
        onProfilePress={onProfilePress}
        onMessagesPress={onMessagesPress}
      />,
      { navigation: false },
    ),
  };
}

describe('TabRootMasthead', () => {
  beforeEach(() => {
    mockSession.isSignedIn = false;
    mockSession.displayName = null;
    mockUnread.count = 0;
  });

  it('shows the signed-out affordance and no messages icon', () => {
    renderMasthead();

    expect(screen.getByTestId(testIds.tabMasthead)).toBeOnTheScreen();
    expect(screen.getByLabelText('Profile')).toBeOnTheScreen();
    expect(screen.getByText('·')).toBeOnTheScreen();
    expect(screen.queryByTestId(testIds.homeMessages)).not.toBeOnTheScreen();
    expect(screen.queryByText('3')).not.toBeOnTheScreen();
    expect(screen.queryByText('99+')).not.toBeOnTheScreen();
  });

  it('shows the messages icon without a badge when signed in with zero unread', () => {
    mockSession.isSignedIn = true;
    mockSession.displayName = 'Contract Member';
    mockUnread.count = 0;
    renderMasthead();

    expect(screen.getByLabelText('Profile')).toBeOnTheScreen();
    expect(screen.getByLabelText('Messages')).toBeOnTheScreen();
    expect(screen.getByText('CM')).toBeOnTheScreen();
    expect(screen.queryByText('3')).not.toBeOnTheScreen();
    expect(screen.queryByText('99+')).not.toBeOnTheScreen();
  });

  it('badges the messages icon, not the avatar, when signed in with waiting PMs', () => {
    mockSession.isSignedIn = true;
    mockSession.displayName = 'Contract Member';
    mockUnread.count = 3;
    renderMasthead();

    expect(screen.getByLabelText('Profile')).toBeOnTheScreen();
    expect(screen.queryByLabelText('Profile, 3 unread conversations')).not.toBeOnTheScreen();
    expect(screen.getByLabelText('Messages, 3 unread conversations')).toBeOnTheScreen();
    expect(screen.getByText('CM')).toBeOnTheScreen();
    expect(screen.getByText('3', { includeHiddenElements: true })).toBeOnTheScreen();
  });

  it('caps the messages badge at 99+', () => {
    mockSession.isSignedIn = true;
    mockSession.displayName = 'Contract Member';
    mockUnread.count = 100;
    renderMasthead();

    expect(screen.getByLabelText('Messages, 100 unread conversations')).toBeOnTheScreen();
    expect(screen.getByText('99+', { includeHiddenElements: true })).toBeOnTheScreen();
  });

  it('hides the messages icon when signed out even if a count leaked', () => {
    mockSession.isSignedIn = false;
    mockUnread.count = 4;
    renderMasthead();

    expect(screen.getByLabelText('Profile')).toBeOnTheScreen();
    expect(screen.getByText('·')).toBeOnTheScreen();
    expect(screen.queryByTestId(testIds.homeMessages)).not.toBeOnTheScreen();
    expect(screen.queryByText('4')).not.toBeOnTheScreen();
  });

  it('omits search and messages when those slots are not provided', () => {
    mockSession.isSignedIn = true;
    mockSession.displayName = 'Contract Member';
    renderWithProviders(<TabRootMasthead onProfilePress={jest.fn()} />, { navigation: false });

    expect(screen.queryByTestId(testIds.homeSearch)).not.toBeOnTheScreen();
    expect(screen.queryByTestId(testIds.homeMessages)).not.toBeOnTheScreen();
    expect(screen.getByTestId(testIds.homeProfile)).toBeOnTheScreen();
  });

  it('opens search, messages, and profile from the shared controls', async () => {
    mockSession.isSignedIn = true;
    mockSession.displayName = 'Contract Member';
    const { onSearch, onProfilePress, onMessagesPress } = renderMasthead();
    const user = userEvent.setup();

    await user.press(screen.getByTestId(testIds.homeSearch));
    await user.press(screen.getByTestId(testIds.homeMessages));
    await user.press(screen.getByTestId(testIds.homeProfile));

    expect(onSearch).toHaveBeenCalledTimes(1);
    expect(onMessagesPress).toHaveBeenCalledTimes(1);
    expect(onProfilePress).toHaveBeenCalledTimes(1);
  });
});
