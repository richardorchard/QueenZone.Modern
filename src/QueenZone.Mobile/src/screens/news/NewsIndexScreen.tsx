import { useCallback } from 'react';
import { FlatList, RefreshControl, StyleSheet } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { fetchNewsPage, formatPublishedDate, type NewsListItem } from '../../api';
import { usePagedContent } from '../../hooks/usePagedContent';
import type { NewsStackParamList } from '../../navigation/types';
import { ArticleRow } from '../../ui/ArticleRow';
import { ListScreenHeader } from '../../ui/ListScreenHeader';
import { EmptyBlock, ErrorBlock, ListFooterLoading, LoadingBlock } from '../../ui/ScreenStates';
import { useTheme } from '../../theme';

type Props = NativeStackScreenProps<NewsStackParamList, 'NewsIndex'>;

export function NewsIndexScreen({ navigation }: Props) {
  const { c } = useTheme();
  const paged = usePagedContent<NewsListItem>(
    useCallback(
      (page, signal) => fetchNewsPage({ page, pageSize: 20, signal }),
      [],
    ),
  );

  if (paged.loading && paged.items.length === 0) {
    return (
      <>
        <ListScreenHeader eyebrow="Archive" title="News" headerShown={false} />
        <LoadingBlock label="Loading news…" />
      </>
    );
  }

  if (paged.error && paged.items.length === 0) {
    return (
      <>
        <ListScreenHeader eyebrow="Archive" title="News" headerShown={false} />
        <ErrorBlock message={paged.error} onRetry={paged.reload} />
      </>
    );
  }

  return (
    <FlatList
      style={[styles.list, { backgroundColor: c.surfacePage }]}
      data={paged.items}
      keyExtractor={(item) => String(item.id)}
      ListHeaderComponent={<ListScreenHeader eyebrow="Archive" title="News" headerShown={false} />}
      ListEmptyComponent={<EmptyBlock message="No news articles yet." />}
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
          title={item.title}
          subtitle={item.excerpt}
          meta={formatPublishedDate(item.publishedAt)}
          onPress={() => navigation.navigate('Story', { id: item.id })}
          accessibilityLabel={`Open ${item.title}`}
        />
      )}
    />
  );
}

const styles = StyleSheet.create({
  list: { flex: 1 },
});
