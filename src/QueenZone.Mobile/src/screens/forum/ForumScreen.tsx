import type { BottomTabScreenProps } from '@react-navigation/bottom-tabs';
import type { CompositeScreenProps } from '@react-navigation/native';
import { useCallback, useMemo } from 'react';
import { FlatList, Platform, Pressable, RefreshControl, StyleSheet, Text, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { Plus } from 'lucide-react-native';
import { fetchForumCategories, type ForumCategoryListItem } from '../../api';
import { usePagedContent } from '../../hooks/usePagedContent';
import type { ForumStackParamList, RootTabParamList } from '../../navigation/types';
import { useSession } from '../../session/SessionContext';
import { openForumComposer } from '../../session/signInNavigation';
import { shadow, space, type, useTheme } from '../../theme';
import { ArticleRow } from '../../ui/ArticleRow';
import { EmptyBlock, ErrorBlock, ListFooterLoading, LoadingBlock } from '../../ui/ScreenStates';
import { PageTitleBlock } from '../../ui/PageTitleBlock';
import { SectionHeader } from '../../ui/SectionHeader';
import { testIds } from '../../test/testIds';
import { TabRootMasthead } from '../home/TabRootMasthead';
import { categoryMeta, formatForumCount } from './forumListMeta';

type Props = CompositeScreenProps<
  NativeStackScreenProps<ForumStackParamList, 'ForumIndex'>,
  BottomTabScreenProps<RootTabParamList>
>;

const categoryPageSize = 50;

export function ForumScreen({ navigation }: Props) {
  const { c, chrome } = useTheme();
  const { isSignedIn } = useSession();
  const fabSize = chrome.android.fabSize ?? 58;
  const paged = usePagedContent<ForumCategoryListItem>(
    useCallback(
      (page, signal) => fetchForumCategories({ page, pageSize: categoryPageSize, signal }),
      [],
    ),
    categoryPageSize,
  );

  const stats = useMemo(() => {
    const postCount = paged.items.reduce((sum, item) => sum + item.postCount, 0);
    return [
      { value: formatForumCount(postCount), label: 'Posts' },
      { value: formatForumCount(paged.totalCount), label: 'Boards' },
    ];
  }, [paged.items, paged.totalCount]);

  const compose = () => {
    openForumComposer(navigation, isSignedIn, {});
  };

  const header = (
    <View>
      <TabRootMasthead
        onSearch={() => navigation.navigate('Search')}
        onProfilePress={() => navigation.navigate('HomeTab', { screen: 'Profile' })}
      />
      <PageTitleBlock eyebrow="Community" title="Forum" />
      <View
        style={{
          flexDirection: 'row',
          paddingHorizontal: space.xl,
          paddingBottom: space.xl,
          gap: space.xl,
        }}
      >
        {stats.map((stat) => (
          <View key={stat.label} style={{ flex: 1, gap: 6 }}>
            <Text style={[type.pageTitle, { fontSize: 22, lineHeight: 26, color: c.textPrimary }]}>
              {stat.value}
            </Text>
            <Text style={[type.eyebrow, { fontSize: 9.5, color: c.textMuted }]}>{stat.label}</Text>
          </View>
        ))}
      </View>
      <SectionHeader title="Boards" />
    </View>
  );

  const body =
    paged.loading && paged.items.length === 0 ? (
      <>
        {header}
        <LoadingBlock label="Loading forum boards…" />
      </>
    ) : paged.error && paged.items.length === 0 ? (
      <>
        {header}
        <ErrorBlock message={paged.error} onRetry={paged.reload} />
      </>
    ) : (
      <FlatList
        style={[styles.list, { backgroundColor: c.surfacePage }]}
        data={paged.items}
        keyExtractor={(item) => String(item.id)}
        ListHeaderComponent={header}
        ListEmptyComponent={<EmptyBlock message="No forum boards are available yet." />}
        ListFooterComponent={<ListFooterLoading visible={paged.loadingMore} />}
        refreshControl={
          <RefreshControl
            refreshing={paged.refreshing}
            onRefresh={paged.refresh}
            tintColor={c.accentPrimary}
          />
        }
        onEndReached={paged.loadMore}
        onEndReachedThreshold={0.4}
        renderItem={({ item }) => (
          <ArticleRow
            title={item.name}
            subtitle={item.description ?? undefined}
            meta={categoryMeta(item)}
            onPress={() => navigation.navigate('Category', { id: item.id, name: item.name })}
            accessibilityLabel={`Open board ${item.name}`}
            testID={`forum-board-${item.id}`}
          />
        )}
      />
    );

  return (
    <View testID={testIds.forumScreen} style={{ flex: 1, backgroundColor: c.surfacePage }}>
      {body}
      {Platform.OS === 'android' ? (
        <Pressable
          testID={testIds.forumNewThread}
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

const styles = StyleSheet.create({
  list: { flex: 1 },
});
