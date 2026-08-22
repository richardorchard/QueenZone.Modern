import { useCallback } from 'react';
import { FlatList, RefreshControl, StyleSheet } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { fetchFanPerformancesPage, formatPublishedDate, type FanPerformance } from '../../api';
import { formatTrackDuration } from '../../audio/formatDuration';
import { usePagedContent } from '../../hooks/usePagedContent';
import type { ArchiveStackParamList } from '../../navigation/types';
import { useSession } from '../../session/SessionContext';
import { ArticleRow } from '../../ui/ArticleRow';
import { EmptyBlock, ErrorBlock, ListFooterLoading, LoadingBlock } from '../../ui/ScreenStates';
import { useTheme } from '../../theme';

type Props = NativeStackScreenProps<ArchiveStackParamList, 'FanPerformances'>;

function metaLine(item: FanPerformance): string {
  const parts = [formatPublishedDate(item.dateAdded), formatTrackDuration(item.durationSeconds)].filter(
    Boolean,
  );
  return parts.join(' · ');
}

export function FanPerformancesScreen({ navigation }: Props) {
  const { c } = useTheme();
  const { isSignedIn } = useSession();
  const paged = usePagedContent<FanPerformance>(
    useCallback((page, signal) => fetchFanPerformancesPage({ page, pageSize: 20, signal }), []),
  );

  if (paged.loading && paged.items.length === 0) {
    return <LoadingBlock label="Loading fan performances…" />;
  }

  if (paged.error && paged.items.length === 0) {
    return <ErrorBlock message={paged.error} onRetry={paged.reload} />;
  }

  return (
    <FlatList
      style={[styles.list, { backgroundColor: c.surfacePage }]}
      data={paged.items}
      keyExtractor={(item) => String(item.id)}
      ListEmptyComponent={<EmptyBlock message="No fan performances are available yet." />}
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
          subtitle={`Performed by ${item.performedBy}`}
          meta={metaLine(item)}
          onPress={() => navigation.navigate('FanPerformanceDetail', { id: item.id })}
          accessibilityLabel={
            isSignedIn
              ? `Play ${item.title}`
              : `Open ${item.title}. Sign in to stream audio.`
          }
        />
      )}
    />
  );
}

const styles = StyleSheet.create({
  list: { flex: 1 },
});
