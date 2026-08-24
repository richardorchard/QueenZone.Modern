import { useCallback, useMemo, useState } from 'react';
import { FlatList, RefreshControl, ScrollView, StyleSheet, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { fetchNewsPage, formatPublishedDate, type NewsListItem } from '../../api';
import { newsDecades, newsYearOptions, type NewsYearOption } from '../../content/sample';
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

type ArchiveFilter = { label: string; decadeStart: number | null; year?: number };

export function NewsIndexScreen({ navigation }: Props) {
  const { c } = useTheme();
  const [filter, setFilter] = useState<ArchiveFilter>(newsDecades[0]);
  const yearOptions = useMemo(() => newsYearOptions(), []);
  const activeYearOption: NewsYearOption =
    yearOptions.find((option) => option.year === filter.year) ?? { label: '', year: 0 };

  const paged = usePagedContent<NewsListItem>(
    useCallback(
      (page, signal) =>
        fetchNewsPage({ page, pageSize: 20, decade: filter.decadeStart ?? undefined, year: filter.year, signal }),
      [filter],
    ),
    20,
    filter.label,
  );

  const countLine =
    paged.totalCount > 0
      ? `${paged.totalCount.toLocaleString('en-GB')} articles · restored from Queenzone.com`
      : 'Restored from Queenzone.com';

  const emptyMessage = filter.year
    ? `No articles for ${filter.year} yet.`
    : filter.decadeStart !== null
      ? 'No articles for this decade yet.'
      : 'No news articles yet.';

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
            active={filter.label === option.label}
            onPress={() => setFilter(option)}
          />
        ))}
      </ScrollView>
    </View>
  );

  const rail =
    yearOptions.length > 1 ? (
      <YearRail
        options={yearOptions}
        value={activeYearOption}
        onChange={(option) => setFilter({ label: option.label, decadeStart: null, year: option.year })}
        testID="news-year-rail"
      />
    ) : null;

  if (paged.loading && paged.items.length === 0) {
    return (
      <View style={styles.wrapper}>
        <View testID={testIds.newsScreen} style={[styles.list, { backgroundColor: c.surfacePage }]}>
          {header}
          <LoadingBlock label="Loading news…" />
        </View>
        {rail}
      </View>
    );
  }

  if (paged.error && paged.items.length === 0) {
    return (
      <View style={styles.wrapper}>
        <View style={[styles.list, { backgroundColor: c.surfacePage }]}>
          {header}
          <ErrorBlock message={paged.error} onRetry={paged.reload} />
        </View>
        {rail}
      </View>
    );
  }

  return (
    <View style={styles.wrapper}>
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
      {rail}
    </View>
  );
}

const styles = StyleSheet.create({
  wrapper: { flex: 1 },
  list: { flex: 1 },
});
