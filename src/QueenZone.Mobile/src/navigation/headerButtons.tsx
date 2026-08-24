import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { ArrowLeft, ChevronLeft, Plus, Search, X } from 'lucide-react-native';
import { Platform, Pressable, Text, View } from 'react-native';
import { useSession } from '../session/SessionContext';
import { openForumComposer } from '../session/signInNavigation';
import { fonts, useTheme } from '../theme';
import { testIds } from '../test/testIds';
import { IconButton } from '../ui/IconButton';
import type { ForumStackParamList } from './types';

export function HeaderBackButton({
  onPress,
  testID,
}: {
  onPress: () => void;
  testID: string;
}) {
  return (
    <IconButton
      icon={Platform.OS === 'android' ? ArrowLeft : ChevronLeft}
      testID={testID}
      accessibilityLabel="Back"
      tone="accent"
      onPress={onPress}
    />
  );
}

export function HeaderCloseButton({
  onPress,
  testID,
}: {
  onPress: () => void;
  testID: string;
}) {
  return (
    <IconButton
      icon={X}
      testID={testID}
      accessibilityLabel="Close"
      tone="accent"
      onPress={onPress}
    />
  );
}

export function SearchHeaderButton({ onPress }: { onPress: () => void }) {
  return <IconButton icon={Search} accessibilityLabel="Search" onPress={onPress} />;
}

export function ComposeHeaderButton({ onPress }: { onPress: () => void }) {
  const { c } = useTheme();

  if (Platform.OS === 'android') {
    return (
      <IconButton
        icon={Plus}
        testID={testIds.forumNewThread}
        accessibilityLabel="New thread"
        onPress={onPress}
      />
    );
  }

  return (
    <Pressable
      testID={testIds.forumNewThread}
      accessibilityRole="button"
      accessibilityLabel="New thread"
      onPress={onPress}
      hitSlop={8}
      style={{ minHeight: 44, justifyContent: 'center', paddingHorizontal: 8 }}
    >
      <Text
        style={{
          fontFamily: fonts.bodyMedium,
          fontSize: 17,
          color: c.accentPrimary,
        }}
      >
        New
      </Text>
    </Pressable>
  );
}

export function ForumHeaderRight({
  onSearch,
  onCompose,
}: {
  onSearch: () => void;
  onCompose: () => void;
}) {
  return (
    <View style={{ flexDirection: 'row', alignItems: 'center' }}>
      {Platform.OS === 'ios' ? <ComposeHeaderButton onPress={onCompose} /> : null}
      <SearchHeaderButton onPress={onSearch} />
    </View>
  );
}

export function ForumIndexHeaderRight({
  navigation,
}: {
  navigation: NativeStackNavigationProp<ForumStackParamList, 'ForumIndex'>;
}) {
  const { isSignedIn } = useSession();
  return (
    <ForumHeaderRight
      onSearch={() => navigation.navigate('Search')}
      onCompose={() => openForumComposer(navigation, isSignedIn, {})}
    />
  );
}
