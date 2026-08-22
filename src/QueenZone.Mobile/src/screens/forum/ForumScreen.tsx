import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { Plus } from 'lucide-react-native';
import { Platform, Pressable, ScrollView, Text, View } from 'react-native';
import { forumStats, sampleThreads } from '../../content/sample';
import type { ForumStackParamList } from '../../navigation/types';
import { shadow, space, type, useTheme } from '../../theme';
import { PageTitleBlock } from '../../ui/PageTitleBlock';
import { SectionHeader } from '../../ui/SectionHeader';
import { ThreadRow } from '../../ui/ThreadRow';

type Props = NativeStackScreenProps<ForumStackParamList, 'ForumIndex'>;

export function ForumScreen({ navigation }: Props) {
  const { c, chrome } = useTheme();
  const fabSize = chrome.android.fabSize ?? 58;

  const compose = () => {
    navigation.navigate('Composer', {});
  };

  return (
    <View style={{ flex: 1, backgroundColor: c.surfacePage }}>
      <ScrollView style={{ flex: 1 }}>
        <PageTitleBlock eyebrow="Community" title="Forum" />
        <View
          style={{
            flexDirection: 'row',
            paddingHorizontal: space.xl,
            paddingBottom: space.xl,
            gap: space.xl,
          }}
        >
          {forumStats.map((stat) => (
            <View key={stat.label} style={{ flex: 1, gap: 6 }}>
              <Text style={[type.pageTitle, { fontSize: 22, lineHeight: 26, color: c.textPrimary }]}>
                {stat.value}
              </Text>
              <Text style={[type.eyebrow, { fontSize: 9.5, color: c.textMuted }]}>{stat.label}</Text>
            </View>
          ))}
        </View>
        <SectionHeader title="Recent threads" />
        {sampleThreads.map((thread) => (
          <ThreadRow
            key={thread.id}
            item={thread}
            onPress={() => navigation.navigate('Thread', { id: thread.id })}
          />
        ))}
        <View style={{ height: space.section }} />
      </ScrollView>
      {Platform.OS === 'android' ? (
        <Pressable
          accessibilityRole="button"
          accessibilityLabel="New thread"
          onPress={compose}
          style={{
            position: 'absolute',
            right: space.xl,
            bottom: space.xl,
            width: fabSize,
            height: fabSize,
            borderRadius: 18,
            backgroundColor: c.accentPrimary,
            alignItems: 'center',
            justifyContent: 'center',
            ...shadow.fab,
          }}
        >
          <Plus size={24} color={c.textOnAccent} strokeWidth={1.5} />
        </Pressable>
      ) : null}
    </View>
  );
}
