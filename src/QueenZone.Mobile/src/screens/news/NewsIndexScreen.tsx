import { useCallback, useState } from 'react';
import { FlatList, RefreshControl, ScrollView, StyleSheet, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { fetchNewsPage, formatPublishedDate, type NewsListItem } from '../../api';
import { newsDecades } from '../../content/sample';
import { usePagedContent } from '../../hooks/usePagedContent';
import type { NewsStackParamList } from '../../navigation/types';
import { space, useTheme } from '../../theme';
import { ArticleRow } from '../../ui/ArticleRow';
import { Chip } from '../../ui/Chip';
import { EmptyBlock, ErrorBlock, ListFooterLoading, LoadingBlock } from '../../ui/ScreenStates';
import { PageTitleBlock } from '../../ui/PageTitleBlock';

type Props = NativeStackScreenProps<NewsStackParamList, 'NewsIndex'>;

export function NewsIndexScreen({ navigation }: Props) {
  const { c } = useTheme();
  const [decade, setDecade] = useState<(typeof newsDecades)[number]>(newsDecades[0]);
  const paged = usePagedContent<NewsListItem>(
    useCallback(
      (page, signal) => fetchNewsPage({ page, pageSize: 20, decade: decade.decadeStart ?? undefined, signal }),
      [decade],
    ),
    20,
    decade.label,
  );

  const countLine =
    paged.totalCount > 0
      ? `${paged.totalCount.toLocaleString('en-GB')} articles · restored from Queenzone.com`
      : 'Restored from Queenzone.com';

  const header = (
    <View>
      <PageTitleBlock eyebrow="The archive" title="News" subtitle={countLine} />
      <ScrollView
        horizontal
        showsHorizontalScrollIndicator={false}
        contentContainerStyle={{ paddingHorizontal: space.xl, gap: 8, paddingBottom: 18 }}
      >
        {newsDecades.map((option) => (
          <Chip
            key={option.label}
            label={option.label}
            active={decade.label === option.label}
            onPress={() => setDecade(option)}
          />
        ))}
      </ScrollView>
    </View>
  );

  if (paged.loading && paged.items.length === 0) {
    return (
      <View style={[styles.list, { backgroundColor: c.surfacePage }]}>
        {header}
        <LoadingBlock label="Loading news…" />
      </View>
    );
  }

  if (paged.error && paged.items.length === 0) {
    return (
      <View style={[styles.list, { backgroundColor: c.surfacePage }]}>
        {header}
        <ErrorBlock message={paged.error} onRetry={paged.reload} />
      </View>
    );
  }

  return (
    <FlatList
      style={[styles.list, { backgroundColor: c.surfacePage }]}
      data={paged.items}
      keyExtractor={(item) => String(item.id)}
      ListHeaderComponent={header}
      ListEmptyComponent={
        <EmptyBlock message={decade.decadeStart === null ? 'No news articles yet.' : 'No articles for this decade yet.'} />
      }
      ListFooterComponent={<ListFooterLoading visible={paged.loadingMore} />}
      refreshControl={
        <RefreshControl refreshing={paged.refreshing} onRefresh={paged.refresh} tintColor={c.accentPrimary} />
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
