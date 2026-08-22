import { Plus, Search } from 'lucide-react-native';
import { Platform, Pressable, Text, View } from 'react-native';
import { fonts, useTheme } from '../theme';
import { IconButton } from '../ui/IconButton';

export function SearchHeaderButton({ onPress }: { onPress: () => void }) {
  return <IconButton icon={Search} accessibilityLabel="Search" onPress={onPress} />;
}

export function ComposeHeaderButton({ onPress }: { onPress: () => void }) {
  const { c } = useTheme();

  if (Platform.OS === 'android') {
    return <IconButton icon={Plus} accessibilityLabel="New thread" onPress={onPress} />;
  }

  return (
    <Pressable
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
