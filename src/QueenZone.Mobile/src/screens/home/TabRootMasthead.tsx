import { Search } from 'lucide-react-native';
import { Pressable, Text, View } from 'react-native';
import { media } from '../../content/media';
import { useSession } from '../../session/SessionContext';
import { fonts, space, useTheme } from '../../theme';
import { testIds } from '../../test/testIds';
import { ArchiveImage } from '../../ui/ArchiveImage';
import { IconButton } from '../../ui/IconButton';
import { initials } from '../../ui/initials';
import { profileA11yLabel } from '../messages/inboxMeta';
import { useUnreadConversationCount } from '../messages/useUnreadConversationCount';

type Props = {
  onProfilePress: () => void;
  onSearch?: () => void;
  topInset?: number;
};

export function TabRootMasthead({ onProfilePress, onSearch, topInset = 0 }: Props) {
  const { c, mode } = useTheme();
  const { isSignedIn, displayName } = useSession();
  const unreadCount = useUnreadConversationCount();
  const avatar = isSignedIn ? initials(displayName) : '';

  return (
    <View
      testID={testIds.tabMasthead}
      style={{
        paddingTop: topInset + 10,
        paddingHorizontal: space.xl,
        paddingBottom: space.md,
        flexDirection: 'row',
        alignItems: 'center',
        justifyContent: 'space-between',
        backgroundColor: c.surfacePage,
      }}
    >
      <View style={{ flexDirection: 'row', alignItems: 'center', gap: 9 }}>
        <ArchiveImage
          source={mode === 'dark' ? media.crestWhite : media.crestBlack}
          label="Queenzone crest"
          style={{ height: 24, width: 24 }}
          contentFit="contain"
        />
        <Text
          style={{
            fontFamily: fonts.titling,
            fontSize: 13,
            fontWeight: '600',
            letterSpacing: 2.3,
            textTransform: 'uppercase',
            color: c.textPrimary,
          }}
        >
          Queenzone
        </Text>
      </View>
      <View style={{ flexDirection: 'row', alignItems: 'center' }}>
        {onSearch ? (
          <IconButton
            icon={Search}
            testID={testIds.homeSearch}
            accessibilityLabel="Search"
            onPress={onSearch}
          />
        ) : null}
        <Pressable
          testID={testIds.homeProfile}
          accessibilityRole="button"
          accessibilityLabel={profileA11yLabel(isSignedIn ? unreadCount : 0)}
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
          {isSignedIn && unreadCount > 0 ? (
            <View
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
      </View>
    </View>
  );
}
