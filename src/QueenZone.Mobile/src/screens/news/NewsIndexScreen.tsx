import { useCallback, useEffect, useState } from 'react';
import { FlatList, RefreshControl, ScrollView, StyleSheet, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { fetchNewsPage, fetchNewsYearRange, formatPublishedDate, type NewsListItem, type NewsYearRange } from '../../api';
import { newsDecades } from '../../content/sample';
import { useNewsListEpochRefresh } from '../../hooks/useNewsListEpochRefresh';
import { usePagedContent } from '../../hooks/usePagedContent';
import type { NewsStackParamList } from '../../navigation/types';
import { space, useTheme } from '../../theme';
import { ArticleRow } from '../../ui/ArticleRow';
import { Chip } from '../../ui/Chip';
import { EmptyBlock, ErrorBlock, ListFooterLoading, LoadingBlock } from '../../ui/ScreenStates';
import { testIds } from '../../test/testIds';
import { PageTitleBlock } from '../../ui/PageTitleBlock';
import { YearRail } from '../../ui/YearRail';

type Props = NativeStackScreenProps<NewsStackParamList, 'NewsIndex'>;

export function newsListResetKey(listKey: string, refreshAt: number | undefined): string {
  return refreshAt === undefined ? listKey : `${listKey}:${refreshAt}`;
}

export function NewsIndexScreen({ navigation, route }: Props) {
  const { c } = useTheme();
  const [decade, setDecade] = useState<(typeof newsDecades)[number]>(newsDecades[0]);
  const [selectedYear, setSelectedYear] = useState<number | null>(null);
  const [yearRange, setYearRange] = useState<NewsYearRange | null>(null);
  const refreshAt = route.params?.refreshAt;

  useEffect(() => {
    const controller = new AbortController();
    fetchNewsYearRange(controller.signal)
      .then(setYearRange)
      .catch(() => undefined);
    return () => controller.abort();
  }, []);

  const paged = usePagedContent<NewsListItem>(
    useCallback(
      (page, signal) =>
        fetchNewsPage({
          page,
          pageSize: 20,
          decade: selectedYear === null ? decade.decadeStart ?? undefined : undefined,
          year: selectedYear ?? undefined,
          signal,
        }),
      [decade, selectedYear],
    ),
    20,
    newsListResetKey(selectedYear === null ? decade.label : `year-${selectedYear}`, refreshAt),
  );
  useNewsListEpochRefresh(paged.refresh);

  const selectDecade = useCallback((option: (typeof newsDecades)[number]) => {
    setSelectedYear(null);
    setDecade(option);
  }, []);

  const selectYear = useCallback((year: number) => {
    setSelectedYear(year);
  }, []);

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
            active={selectedYear === null && decade.label === option.label}
            onPress={() => selectDecade(option)}
          />
        ))}
      </ScrollView>
    </View>
  );

  if (paged.loading && paged.items.length === 0) {
    return (
      <View testID={testIds.newsScreen} style={[styles.list, { backgroundColor: c.surfacePage }]}>
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

  const emptyMessage = selectedYear !== null
    ? `No articles for ${selectedYear} yet.`
    : decade.decadeStart === null
      ? 'No news articles yet.'
      : 'No articles for this decade yet.';

  return (
    <View style={styles.container}>
      <FlatList
        testID={testIds.newsScreen}
        style={[styles.list, { backgroundColor: c.surfacePage }]}
        data={paged.items}
        keyExtractor={(item) => String(item.id)}
        ListHeaderComponent={header}
        ListEmptyComponent={<EmptyBlock message={emptyMessage} />}
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
            testID={`news-story-${item.id}`}
          />
        )}
      />
      <YearRail
        minYear={yearRange?.minYear ?? null}
        maxYear={yearRange?.maxYear ?? null}
        activeYear={selectedYear}
        onSelectYear={selectYear}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1 },
  list: { flex: 1 },
});
