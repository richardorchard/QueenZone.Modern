import { useCallback, useEffect, useLayoutEffect, useState } from 'react';
import { FlatList, RefreshControl, StyleSheet, Text, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import {
  ApiError,
  fetchForumCategory,
  fetchForumCategoryTopics,
  type ForumCategoryListItem,
  type ForumTopicListItem,
} from '../../api';
import { usePagedContent } from '../../hooks/usePagedContent';
import type { ForumStackParamList } from '../../navigation/types';
import { ArticleRow } from '../../ui/ArticleRow';
import { EmptyBlock, ErrorBlock, ListFooterLoading, LoadingBlock } from '../../ui/ScreenStates';
import { space, type, useTheme } from '../../theme';
import { formatForumCount, topicMeta } from './forumListMeta';

type Props = NativeStackScreenProps<ForumStackParamList, 'Category'>;

/** Matches the website category page (`ForumRoutes.TopicsPageSize`). */
const topicPageSize = 25;

function messageFromUnknownError(err: unknown): string {
  return err instanceof ApiError ? err.message : 'Something went wrong.';
}

export function CategoryScreen({ navigation, route }: Props) {
  const { c } = useTheme();
  const { id, name } = route.params;
  const [category, setCategory] = useState<ForumCategoryListItem | null>(null);
  const [categoryError, setCategoryError] = useState<string | null>(null);
  const [categoryReloadToken, setCategoryReloadToken] = useState(0);

  const paged = usePagedContent<ForumTopicListItem>(
    useCallback(
      (page, signal) => fetchForumCategoryTopics(id, { page, pageSize: topicPageSize, signal }),
      [id],
    ),
    topicPageSize,
  );

  useEffect(() => {
    const controller = new AbortController();
    setCategoryError(null);
    fetchForumCategory(id, controller.signal)
      .then((item) => {
        setCategory(item);
        setCategoryError(null);
      })
      .catch((err: unknown) => {
        if (err instanceof Error && err.name === 'AbortError') {
          return;
        }
        setCategory(null);
        setCategoryError(messageFromUnknownError(err));
      });
    return () => controller.abort();
  }, [id, categoryReloadToken]);

  useLayoutEffect(() => {
    navigation.setOptions({ title: category?.name ?? name ?? 'Board' });
  }, [navigation, category?.name, name]);

  const retryCategory = useCallback(() => setCategoryReloadToken((n) => n + 1), []);

  const retry = useCallback(() => {
    retryCategory();
    paged.reload();
  }, [retryCategory, paged]);

  const refresh = useCallback(() => {
    retryCategory();
    paged.refresh();
  }, [retryCategory, paged]);

  const stats = [
    paged.totalCount > 0 ? `${formatForumCount(paged.totalCount)} threads` : null,
    category ? `${formatForumCount(category.postCount)} posts` : null,
  ]
    .filter(Boolean)
    .join(' · ');

  const header = (
    <View style={styles.header}>
      {category?.description ? (
        <Text style={[type.caption, { color: c.textSecondary }]}>{category.description}</Text>
      ) : null}
      {stats ? <Text style={[type.meta, { color: c.textMuted, marginTop: space.xs }]}>{stats}</Text> : null}
    </View>
  );

  const categoryPending = !category && !categoryError;
  if ((paged.loading && paged.items.length === 0) || categoryPending) {
    return <LoadingBlock label="Loading topics…" />;
  }

  if (categoryError) {
    return <ErrorBlock message={categoryError} onRetry={retry} />;
  }

  if (paged.error && paged.items.length === 0) {
    return <ErrorBlock message={paged.error} onRetry={retry} />;
  }

  return (
    <FlatList
      style={[styles.list, { backgroundColor: c.surfacePage }]}
      data={paged.items}
      keyExtractor={(item) => String(item.id)}
      ListHeaderComponent={header}
      ListEmptyComponent={<EmptyBlock message="No topics are available in this board yet." />}
      ListFooterComponent={<ListFooterLoading visible={paged.loadingMore} />}
      refreshControl={
        <RefreshControl
          refreshing={paged.refreshing}
          onRefresh={refresh}
          tintColor={c.accentPrimary}
        />
      }
      onEndReached={paged.loadMore}
      onEndReachedThreshold={0.4}
      renderItem={({ item }) => (
        <ArticleRow
          title={item.title}
          subtitle={item.authorUsername}
          meta={topicMeta(item)}
          onPress={() => navigation.navigate('Thread', { id: item.id, title: item.title })}
          accessibilityLabel={`Open thread ${item.title}`}
        />
      )}
    />
  );
}

const styles = StyleSheet.create({
  list: { flex: 1 },
  header: {
    paddingHorizontal: space.xl,
    paddingTop: space.base,
    paddingBottom: space.sm,
  },
});
