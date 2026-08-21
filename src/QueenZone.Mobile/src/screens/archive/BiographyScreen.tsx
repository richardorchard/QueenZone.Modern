import { useCallback } from 'react';
import { FlatList, RefreshControl, StyleSheet } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { fetchBiographyPage, type BiographyChapterListItem } from '../../api';
import { usePagedContent } from '../../hooks/usePagedContent';
import type { ArchiveStackParamList } from '../../navigation/types';
import { ArticleRow } from '../../ui/ArticleRow';
import { EmptyBlock, ErrorBlock, ListFooterLoading, LoadingBlock } from '../../ui/ScreenStates';
import { useTheme } from '../../theme';

type Props = NativeStackScreenProps<ArchiveStackParamList, 'Biography'>;

export function BiographyScreen({ navigation }: Props) {
  const { c } = useTheme();
  const paged = usePagedContent<BiographyChapterListItem>(
    useCallback((page, signal) => fetchBiographyPage({ page, pageSize: 50, signal }), []),
    50,
  );

  if (paged.loading && paged.items.length === 0) {
    return <LoadingBlock label="Loading biography…" />;
  }

  if (paged.error && paged.items.length === 0) {
    return <ErrorBlock message={paged.error} onRetry={paged.reload} />;
  }

  return (
    <FlatList
      style={[styles.list, { backgroundColor: c.surfacePage }]}
      data={paged.items}
      keyExtractor={(item) => String(item.id)}
      ListEmptyComponent={<EmptyBlock message="No biography chapters yet." />}
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
          subtitle={item.summary}
          meta={`Chapter ${item.displaySequence}`}
          onPress={() => navigation.navigate('BiographyChapter', { id: item.id })}
          accessibilityLabel={`Open chapter ${item.title}`}
        />
      )}
    />
  );
}

const styles = StyleSheet.create({
  list: { flex: 1 },
});
