import { useNavigation } from '@react-navigation/native';
import { ChevronRight, Search } from 'lucide-react-native';
import { useMemo, useState } from 'react';
import { Platform, Pressable, Text, TextInput, View } from 'react-native';
import { searchSuggestions } from '../../content/sample';
import { fonts, space, type, useTheme } from '../../theme';
import { ArchiveFooter } from '../../ui/ArchiveFooter';
import { Eyebrow } from '../../ui/Eyebrow';

type Target = (typeof searchSuggestions)[number]['target'];

type Props = {
  onOpen?: (target: Target) => void;
};

export function SearchScreen({ onOpen }: Props) {
  const { c, chrome } = useTheme();
  const [query, setQuery] = useState('');
  const fieldRadius = Platform.OS === 'ios' ? chrome.ios.searchFieldRadius : chrome.android.searchFieldRadius;
  const results = useMemo(() => {
    const q = query.trim().toLowerCase();
    if (q.length <= 1) {
      return searchSuggestions;
    }
    return searchSuggestions.filter(
      (item) => item.title.toLowerCase().includes(q) || item.tag.toLowerCase().includes(q),
    );
  }, [query]);
  const section = query.trim().length > 1 ? 'Results' : 'Suggested';

  return (
    <View style={{ flex: 1, backgroundColor: c.surfacePage }}>
      <View style={{ paddingHorizontal: space.xl, paddingTop: space.md, paddingBottom: space.lg }}>
        <View
          style={{
            height: 44,
            borderRadius: fieldRadius,
            backgroundColor: '#1D1D1D',
            borderWidth: 1,
            borderColor: c.border,
            flexDirection: 'row',
            alignItems: 'center',
            paddingHorizontal: 12,
            gap: 10,
          }}
        >
          <Search size={18} color={c.textMuted} strokeWidth={1.5} />
          <TextInput
            autoFocus
            value={query}
            onChangeText={setQuery}
            placeholder="Search 4,000+ articles and photographs"
            placeholderTextColor={c.textMuted}
            accessibilityLabel="Search the archive"
            style={{
              flex: 1,
              color: c.textPrimary,
              fontFamily: fonts.body,
              fontSize: 16,
            }}
          />
        </View>
      </View>
      <View style={{ paddingHorizontal: space.xl, paddingBottom: space.md }}>
        <Eyebrow tone="muted">{section}</Eyebrow>
      </View>
      {results.length === 0 ? (
        <Text style={[type.body, { color: c.textSecondary, paddingHorizontal: space.xl }]}>
          Nothing in the archive matches that — yet.
        </Text>
      ) : (
        results.map((item) => (
          <Pressable
            key={item.title}
            accessibilityRole="button"
            accessibilityLabel={`${item.title}. ${item.tag}`}
            onPress={() => onOpen?.(item.target)}
            style={{
              flexDirection: 'row',
              alignItems: 'center',
              paddingHorizontal: space.xl,
              paddingVertical: 16,
              borderTopWidth: 1,
              borderTopColor: c.hairline,
              gap: 12,
            }}
          >
            <View style={{ flex: 1, gap: 6 }}>
              <Text style={[type.listTitle, { color: c.textPrimary }]}>{item.title}</Text>
              <Text
                style={[
                  type.meta,
                  { color: item.editorial ? c.accentPrimary : c.textMuted },
                ]}
              >
                {item.tag.toUpperCase()}
              </Text>
            </View>
            <ChevronRight size={17} color={c.textMuted} strokeWidth={1.5} />
          </Pressable>
        ))
      )}
      <ArchiveFooter />
    </View>
  );
}

export function SearchRouteScreen() {
  const navigation = useNavigation();
  return (
    <SearchScreen
      onOpen={(target) => {
        const root = navigation.getParent();
        if (target === 'story') {
          root?.navigate('ArchiveTab', { screen: 'Story', params: { id: 0 } });
          return;
        }
        if (target === 'photos') {
          root?.navigate('PhotosTab', { screen: 'PhotoIndex' });
          return;
        }
        if (target === 'news') {
          root?.navigate('NewsTab', { screen: 'NewsIndex' });
          return;
        }
        root?.navigate('ForumTab', { screen: 'Thread', params: { id: 'magic-tour' } });
      }}
    />
  );
}
