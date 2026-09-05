import { FlatList, Pressable, StyleSheet, Text, View } from 'react-native';
import { Pause, Play, Trash2 } from 'lucide-react-native';
import { useFanPerformancePlayer } from '../audio/FanPerformancePlayer';
import type { FanPerformance } from '../api';
import { testIds } from '../test/testIds';
import { EmptyBlock } from '../ui/ScreenStates';
import { space, type, useTheme } from '../theme';
import { formatByteSize } from './formatBytes';
import { removeDownload } from './manager';
import { useDownloadMemberId, useDownloadUiList } from './useDownloadUi';
import type { DownloadUiSnapshot } from './types';

function toTrack(item: DownloadUiSnapshot): FanPerformance {
  return {
    id: Number(item.performanceId),
    title: item.title,
    performedBy: item.performedBy,
    description: '',
    dateAdded: '',
    durationSeconds: null,
    detailPath: `/fan-performances/${item.performanceId}`,
    audioPath: `/api/v1/content/fan-performances/${item.performanceId}/audio`,
  };
}

export function FanPerformanceDownloadsList() {
  const { c } = useTheme();
  const memberId = useDownloadMemberId();
  const items = useDownloadUiList().filter((item) => item.status === 'downloaded');
  const player = useFanPerformancePlayer();

  return (
    <FlatList
      testID={testIds.fanPerformanceDownloadsScreen}
      style={[styles.list, { backgroundColor: c.surfacePage }]}
      data={items}
      keyExtractor={(item) => item.performanceId}
      ListEmptyComponent={<EmptyBlock message="No downloaded recordings yet." />}
      renderItem={({ item }) => {
        const track = toTrack(item);
        const playingThis = player.current?.id === track.id && player.playing;
        const sizeLabel = formatByteSize(item.byteSize);
        return (
          <View style={[styles.row, { borderTopColor: c.hairline }]}>
            <Pressable
              testID={`${testIds.fanPerformanceDownloadPlayPrefix}${item.performanceId}`}
              accessibilityRole="button"
              accessibilityLabel={playingThis ? `Pause ${item.title}` : `Play ${item.title} offline`}
              onPress={() => {
                if (player.current?.id === track.id) {
                  player.toggle();
                  return;
                }
                player.play(track, items.map(toTrack));
              }}
              style={[
                styles.play,
                {
                  borderColor: playingThis ? c.textPrimary : c.borderStrong,
                  backgroundColor: playingThis ? c.textPrimary : c.surfaceRaised,
                },
              ]}
            >
              {playingThis ? (
                <Pause size={18} color={c.surfacePage} fill={c.surfacePage} />
              ) : (
                <Play size={18} color={c.textPrimary} fill={c.textPrimary} />
              )}
            </Pressable>
            <View style={styles.copy}>
              <Text style={[type.listTitle, { color: c.textPrimary }]}>{item.title}</Text>
              <Text style={[type.caption, { color: c.textSecondary, marginTop: space.xs }]}>
                Performed by {item.performedBy}
                {sizeLabel ? ` · ${sizeLabel}` : ''}
              </Text>
            </View>
            <Pressable
              testID={`${testIds.fanPerformanceDownloadRemovePrefix}${item.performanceId}`}
              accessibilityRole="button"
              accessibilityLabel={`Remove download of ${item.title}`}
              onPress={() => {
                if (!memberId) {
                  return;
                }
                void removeDownload(memberId, item.performanceId);
              }}
              style={styles.remove}
            >
              <Trash2 size={18} color={c.danger} />
            </Pressable>
          </View>
        );
      }}
    />
  );
}

const styles = StyleSheet.create({
  list: { flex: 1 },
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: space.base,
    paddingHorizontal: space.xl,
    paddingVertical: space.base,
    borderTopWidth: StyleSheet.hairlineWidth,
  },
  play: {
    width: 44,
    height: 44,
    borderRadius: 22,
    borderWidth: 1,
    alignItems: 'center',
    justifyContent: 'center',
  },
  copy: { flex: 1, minWidth: 0 },
  remove: {
    width: 44,
    height: 44,
    alignItems: 'center',
    justifyContent: 'center',
  },
});
