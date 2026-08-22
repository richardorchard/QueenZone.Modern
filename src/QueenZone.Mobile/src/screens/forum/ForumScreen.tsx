import { useCallback } from 'react';
import { FlatList, RefreshControl, StyleSheet, Text, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { fetchForumCategories, type ForumCategoryListItem } from '../../api';
import { usePagedContent } from '../../hooks/usePagedContent';
import type { ForumStackParamList } from '../../navigation/types';
import { ArticleRow } from '../../ui/ArticleRow';
import { ListScreenHeader } from '../../ui/ListScreenHeader';
import { EmptyBlock, ErrorBlock, ListFooterLoading, LoadingBlock } from '../../ui/ScreenStates';
import { space, type, useTheme } from '../../theme';
import { categoryMeta } from './forumListMeta';

type Props = NativeStackScreenProps<ForumStackParamList, 'ForumIndex'>;

const categoryPageSize = 50;

export function ForumScreen({ navigation }: Props) {
  const { c } = useTheme();
  const paged = usePagedContent<ForumCategoryListItem>(
    useCallback(
      (page, signal) => fetchForumCategories({ page, pageSize: categoryPageSize, signal }),
      [],
    ),
    categoryPageSize,
  );

  const header = (
    <View>
      <ListScreenHeader eyebrow="Community archive" title="Forum" headerShown={false} />
      <Text
        style={[type.caption, { color: c.textSecondary, paddingHorizontal: space.xl, paddingBottom: space.base }]}
      >
        Public boards from the website. Starting a thread or posting a reply needs a signed-in member.
      </Text>
    </View>
  );

  if (paged.loading && paged.items.length === 0) {
    return (
      <>
        {header}
        <LoadingBlock label="Loading forum boards…" />
      </>
    );
  }

  if (paged.error && paged.items.length === 0) {
    return (
      <>
        {header}
        <ErrorBlock message={paged.error} onRetry={paged.reload} />
      </>
    );
  }

  return (
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
        />
      )}
    />
  );
}

const styles = StyleSheet.create({
  list: { flex: 1 },
});
