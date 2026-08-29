import { Mail } from 'lucide-react-native';
import { Platform, Pressable, Text, View } from 'react-native';
import { messagesA11yLabel } from '../screens/messages/inboxMeta';
import { useUnreadConversationCount } from '../screens/messages/useUnreadConversationCount';
import { useSession } from '../session/SessionContext';
import { testIds } from '../test/testIds';
import { fonts, useTheme } from '../theme';
import { initials } from '../ui/initials';
import { usePressProps } from '../ui/press';
import { nestedTabParams } from './nestedTab';

type Props = {
  onProfilePress: () => void;
  onMessagesPress: () => void;
};

/** Mail + avatar for News / Forum / Photos / Archive native headerRight. */
export function TabIdentityHeaderRight({ onProfilePress, onMessagesPress }: Props) {
  const { c } = useTheme();
  const { isSignedIn, displayName } = useSession();
  const unreadCount = useUnreadConversationCount();
  const avatar = isSignedIn ? initials(displayName) : '';
  const messagesPress = usePressProps(true);

  return (
    <View
      testID={testIds.tabIdentityHeader}
      style={{ flexDirection: 'row', alignItems: 'center' }}
    >
      {isSignedIn ? (
        <Pressable
          testID={testIds.homeMessages}
          accessibilityRole="button"
          accessibilityLabel={messagesA11yLabel(unreadCount)}
          onPress={onMessagesPress}
          {...messagesPress}
          style={({ pressed }) => [
            { width: 44, height: 44, alignItems: 'center', justifyContent: 'center', borderRadius: 22 },
            Platform.OS === 'ios' && pressed ? { opacity: 0.6 } : null,
          ]}
        >
          <Mail size={20} color={c.textPrimary} strokeWidth={1.5} />
          {unreadCount > 0 ? (
            <View
              testID={testIds.homeMessagesUnread}
              style={{
                position: 'absolute',
                top: 2,
                right: 2,
                minWidth: 16,
                height: 16,
                borderRadius: 8,
                paddingHorizontal: 4,
                backgroundColor: c.accentPrimary,
                alignItems: 'center',
                justifyContent: 'center',
              }}
              importantForAccessibility="no"
              accessibilityElementsHidden
            >
              <Text
                style={{
                  fontFamily: fonts.bodyMedium,
                  fontSize: 9,
                  lineHeight: 11,
                  color: c.textOnAccent,
                }}
              >
                {unreadCount > 99 ? '99+' : unreadCount}
              </Text>
            </View>
          ) : null}
        </Pressable>
      ) : null}
      <Pressable
        testID={testIds.homeProfile}
        accessibilityRole="button"
        accessibilityLabel="Profile"
        onPress={onProfilePress}
        style={{ width: 44, height: 44, alignItems: 'center', justifyContent: 'center' }}
      >
        <View
          style={{
            width: 32,
            height: 32,
            borderRadius: 16,
            backgroundColor: c.surfaceCard,
            borderWidth: 1,
            borderColor: c.border,
            alignItems: 'center',
            justifyContent: 'center',
          }}
        >
          <Text style={{ fontFamily: fonts.bodyMedium, fontSize: 12, color: c.textPrimary }}>
            {avatar || '·'}
          </Text>
        </View>
      </Pressable>
    </View>
  );
}

type HomeTabNavigate = {
  navigate: (name: 'HomeTab', params: { screen: 'Inbox'; initial: false } | { screen: 'Profile' }) => void;
};

/** Same Inbox / Profile destinations the tab-root masthead used on these four roots. */
export function tabIdentityHandlers(navigation: object): {
  onMessagesPress: () => void;
  onProfilePress: () => void;
} {
  const { navigate } = navigation as HomeTabNavigate;
  return {
    onMessagesPress: () => navigate('HomeTab', nestedTabParams('Inbox')),
    onProfilePress: () => navigate('HomeTab', { screen: 'Profile' }),
  };
}
