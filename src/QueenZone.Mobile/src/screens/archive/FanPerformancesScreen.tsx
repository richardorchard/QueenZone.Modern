import { useCallback } from 'react';
import { Pressable, FlatList, RefreshControl, StyleSheet } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { Pause, Play } from 'lucide-react-native';
import { fetchFanPerformancesPage, formatPublishedDate, type FanPerformance } from '../../api';
import { useFanPerformancePlayer } from '../../audio/FanPerformancePlayer';
import { formatTrackDuration } from '../../audio/formatDuration';
import { usePagedContent } from '../../hooks/usePagedContent';
import type { ArchiveStackParamList } from '../../navigation/types';
import { useSession } from '../../session/SessionContext';
import { openSignIn } from '../../session/signInNavigation';
import { testIds } from '../../test/testIds';
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
  const { accessToken, isRestoring } = useSession();
  const player = useFanPerformancePlayer();
  const paged = usePagedContent<FanPerformance>(
    useCallback((page, signal) => fetchFanPerformancesPage({ page, pageSize: 20, signal }), []),
  );
  const canPlay = Boolean(accessToken);

  const onPlay = useCallback(
    (item: FanPerformance) => {
      if (isRestoring) {
        return;
      }
      if (!canPlay) {
        openSignIn(navigation, { tab: 'ArchiveTab', screen: 'FanPerformances' });
        return;
      }
      if (player.current?.id === item.id) {
        player.toggle();
        return;
      }
      player.play(item, paged.items);
    },
    [canPlay, isRestoring, navigation, paged.items, player],
  );

  if (paged.loading && paged.items.length === 0) {
    return <LoadingBlock label="Loading fan performances…" />;
  }

  if (paged.error && paged.items.length === 0) {
    return <ErrorBlock message={paged.error} onRetry={paged.reload} />;
  }

  return (
    <FlatList
      testID={testIds.fanPerformancesScreen}
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
      renderItem={({ item }) => {
        const playingThis = player.current?.id === item.id && player.playing;
        return (
          <ArticleRow
            title={item.title}
            subtitle={`Performed by ${item.performedBy}`}
            meta={metaLine(item)}
            hint={!canPlay && !isRestoring ? 'Sign in to play' : undefined}
            leading={
              <Pressable
                testID={`${testIds.fanPerformancePlayPrefix}${item.id}`}
                accessibilityRole="button"
                accessibilityState={{ selected: playingThis }}
                accessibilityLabel={
                  canPlay
                    ? playingThis
                      ? `Pause ${item.title}`
                      : `Play ${item.title}`
                    : `Sign in to play ${item.title}`
                }
                onPress={() => onPlay(item)}
                style={[
                  styles.play,
                  {
                    borderColor: playingThis ? c.textPrimary : c.borderStrong,
                    backgroundColor: playingThis ? c.textPrimary : c.surfaceRaised,
                  },
                  playingThis ? null : styles.playOffset,
                ]}
              >
                {playingThis ? (
                  <Pause size={20} color={c.surfacePage} fill={c.surfacePage} />
                ) : (
                  <Play size={20} color={c.textPrimary} fill={c.textPrimary} />
                )}
              </Pressable>
            }
            onPress={() => navigation.navigate('FanPerformanceDetail', { id: item.id })}
            accessibilityLabel={`Open ${item.title}`}
            testID={`${testIds.fanPerformanceRowPrefix}${item.id}`}
          />
        );
      }}
    />
  );
}

const styles = StyleSheet.create({
  list: { flex: 1 },
  play: {
    width: 48,
    height: 48,
    borderRadius: 24,
    borderWidth: 1,
    alignItems: 'center',
    justifyContent: 'center',
  },
  playOffset: {
    paddingLeft: 2,
  },
});
