import { FlatList, Pressable, StyleSheet, Text, View } from 'react-native';
import { Pause, Play, Trash2 } from 'lucide-react-native';
import { useFanPerformancePlayer } from '../audio/FanPerformancePlayer';
import type { FanPerformance } from '../api';
import { testIds } from '../test/testIds';
import { EmptyBlock } from '../ui/ScreenStates';
import { space, type, useTheme } from '../theme';
import { DownloadAction } from './DownloadAction';
import { formatByteSize, formatDownloadProgress } from './formatBytes';
import { removeDownload } from './manager';
import { useDownloadMemberId, useDownloadUi, useDownloadUiList } from './useDownloadUi';
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

function statusLine(item: DownloadUiSnapshot): string {
  if (item.status === 'downloading') {
    const progress = formatDownloadProgress(item.byteSize, item.expectedBytes);
    return progress ? `Downloading · ${progress}` : 'Downloading';
  }
  if (item.status === 'queued') {
    return 'Queued';
  }
  if (item.status === 'failed') {
    return item.error ?? 'Download failed';
  }
  const sizeLabel = formatByteSize(item.byteSize);
  return sizeLabel ? `Downloaded · ${sizeLabel}` : 'Downloaded';
}

function progressSignature(item: DownloadUiSnapshot): string {
  return `${item.performanceId}:${item.status}:${item.byteSize ?? ''}:${item.expectedBytes ?? ''}:${item.error ?? ''}`;
}

function FanPerformanceDownloadRow({
  performanceId,
  playQueue,
}: {
  performanceId: string;
  playQueue: FanPerformance[];
}) {
  const { c } = useTheme();
  const memberId = useDownloadMemberId();
  const item = useDownloadUi(performanceId);
  const player = useFanPerformancePlayer();
  if (!item || item.status === 'removing') {
    return null;
  }

  const track = toTrack(item);
  const playingThis = player.current?.id === track.id && player.playing;
  const line = statusLine(item);

  return (
    <View style={[styles.row, { borderTopColor: c.hairline }]}>
      <Pressable
        testID={`${testIds.fanPerformanceDownloadPlayPrefix}${item.performanceId}`}
        accessibilityRole="button"
        accessibilityLabel={playingThis ? `Pause ${item.title}` : `Play ${item.title}`}
        hitSlop={{ top: 8, bottom: 8, left: 8, right: 6 }}
        unstable_pressDelay={0}
        onPress={() => {
          if (player.current?.id === track.id) {
            player.toggle();
            return;
          }
          player.play(track, playQueue.length > 0 ? playQueue : [track]);
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
        <Text
          testID={`${testIds.fanPerformanceDownloadProgressPrefix}${item.performanceId}`}
          style={[type.caption, { color: c.textSecondary, marginTop: space.xs }]}
          numberOfLines={2}
        >
          Performed by {item.performedBy}
          {item.status === 'failed' ? '' : ` · ${line}`}
        </Text>
        {item.status === 'failed' ? (
          <Text
            testID={`${testIds.fanPerformanceDownloadErrorPrefix}${item.performanceId}`}
            style={[type.caption, { color: c.danger, marginTop: space.xs }]}
          >
            {line}
          </Text>
        ) : null}
      </View>
      {item.status === 'downloaded' ? (
        <Pressable
          testID={`${testIds.fanPerformanceDownloadRemovePrefix}${item.performanceId}`}
          accessibilityRole="button"
          accessibilityLabel={`Remove download of ${item.title}`}
          hitSlop={8}
          unstable_pressDelay={0}
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
      ) : (
        <DownloadAction key={item.performanceId} track={track} compact onNeedSignIn={() => undefined} />
      )}
    </View>
  );
}

export function FanPerformanceDownloadsList() {
  const { c } = useTheme();
  const items = useDownloadUiList()
    .filter((item) => item.status !== 'removing')
    .sort((a, b) => a.title.localeCompare(b.title) || a.performanceId.localeCompare(b.performanceId));
  const ids = items.map((item) => item.performanceId);
  const extraData = items.map(progressSignature).join('|');
  const playQueue = items.filter((item) => item.status === 'downloaded').map(toTrack);

  return (
    <FlatList
      testID={testIds.fanPerformanceDownloadsScreen}
      style={[styles.list, { backgroundColor: c.surfacePage }]}
      data={ids}
      extraData={extraData}
      keyExtractor={(performanceId) => performanceId}
      ListEmptyComponent={<EmptyBlock message="No downloaded recordings yet." />}
      renderItem={({ item: performanceId }) => (
        <FanPerformanceDownloadRow performanceId={performanceId} playQueue={playQueue} />
      )}
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
