import { memo, useCallback, useEffect, useLayoutEffect, useState } from 'react';
import { StyleSheet, Text, View, type ListRenderItem } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import {
  ApiError,
  fetchForumCategory,
  fetchForumCategoryTopics,
  type ForumCategoryListItem,
  type ForumTopicListItem,
} from '../../api';
import { usePagedContent } from '../../hooks/usePagedContent';
import { ComposeHeaderButton } from '../../navigation/headerButtons';
import type { ForumStackParamList } from '../../navigation/types';
import { useSession } from '../../session/SessionContext';
import { openForumComposer } from '../../session/signInNavigation';
import { ArticleRow } from '../../ui/ArticleRow';
import { PagedListScreen } from '../../ui/PagedListScreen';
import { ErrorBlock, LoadingBlock } from '../../ui/ScreenStates';
import { testIds } from '../../test/testIds';
import { space, type, useTheme } from '../../theme';
import { formatForumCount, topicMeta } from './forumListMeta';

type Props = NativeStackScreenProps<ForumStackParamList, 'Category'>;

/** Matches the website category page (`ForumRoutes.TopicsPageSize`). */
const topicPageSize = 25;

function topicKeyExtractor(item: ForumTopicListItem): string {
  return String(item.id);
}

const CategoryTopicRow = memo(function CategoryTopicRow({
  item,
  onOpen,
}: {
  item: ForumTopicListItem;
  onOpen: (id: number, title: string) => void;
}) {
  return (
    <ArticleRow
      title={item.title}
      subtitle={item.authorUsername}
      meta={topicMeta(item)}
      onPress={() => onOpen(item.id, item.title)}
      accessibilityLabel={`Open thread ${item.title}`}
      testID={`forum-thread-${item.id}`}
    />
  );
});

function messageFromUnknownError(err: unknown): string {
  return err instanceof ApiError ? err.message : 'Something went wrong.';
}

export function CategoryScreen({ navigation, route }: Props) {
  const { c } = useTheme();
  const { isSignedIn } = useSession();
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
    const boardName = category?.name ?? name;
    navigation.setOptions({
      title: boardName ?? 'Board',
      headerRight: () => (
        <ComposeHeaderButton
          onPress={() =>
            openForumComposer(navigation, isSignedIn, {
              categoryId: id,
              categoryName: boardName,
            })
          }
        />
      ),
    });
  }, [category?.name, id, isSignedIn, name, navigation]);

  const retryCategory = useCallback(() => setCategoryReloadToken((n) => n + 1), []);

  const retry = useCallback(() => {
    retryCategory();
    paged.reload();
  }, [retryCategory, paged]);

  const refresh = useCallback(() => {
    retryCategory();
    paged.refresh();
  }, [retryCategory, paged]);

  const openThread = useCallback(
    (topicId: number, topicTitle: string) => {
      navigation.navigate('Thread', { id: topicId, title: topicTitle });
    },
    [navigation],
  );

  const renderItem = useCallback<ListRenderItem<ForumTopicListItem>>(
    ({ item }) => <CategoryTopicRow item={item} onOpen={openThread} />,
    [openThread],
  );

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

  if (!category && !categoryError) {
    return <LoadingBlock label="Loading topics…" />;
  }

  if (categoryError) {
    return <ErrorBlock message={categoryError} onRetry={retry} />;
  }

  return (
    <PagedListScreen
      testID={testIds.forumCategoryScreen}
      paged={{ ...paged, refresh, reload: retry }}
      keyExtractor={topicKeyExtractor}
      loadingLabel="Loading topics…"
      emptyMessage="No topics are available in this board yet."
      ListHeaderComponent={header}
      renderItem={renderItem}
    />
  );
}

const styles = StyleSheet.create({
  header: {
    paddingHorizontal: space.xl,
    paddingTop: space.base,
    paddingBottom: space.sm,
  },
});
