import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useCallback, useMemo, useState } from 'react';
import { FlatList, Linking, Pressable, RefreshControl, StyleSheet, Text, View } from 'react-native';
import { fetchTimelinePage, type TimelineEvent } from '../../api';
import { usePagedContent } from '../../hooks/usePagedContent';
import type { ArchiveStackParamList } from '../../navigation/types';
import { EmptyBlock, ErrorBlock, ListFooterLoading, LoadingBlock } from '../../ui/ScreenStates';
import { space, type, useTheme } from '../../theme';

type Props = NativeStackScreenProps<ArchiveStackParamList, 'Timeline'>;

type DecadeSection = {
  decade: string;
  events: TimelineEvent[];
};

function decadeLabel(iso: string): string {
  const year = new Date(iso).getFullYear();
  if (Number.isNaN(year)) {
    return 'Undated';
  }
  const start = Math.floor(year / 10) * 10;
  return `${start}s`;
}

export function TimelineScreen({ route }: Props) {
  const { c } = useTheme();
  const [expandedId, setExpandedId] = useState<number | null>(route.params?.focusId ?? null);
  const paged = usePagedContent<TimelineEvent>(
    useCallback((page, signal) => fetchTimelinePage({ page, pageSize: 100, signal }), []),
    100,
  );

  const sections = useMemo(() => {
    const map = new Map<string, TimelineEvent[]>();
    for (const event of paged.items) {
      const key = decadeLabel(event.eventDate);
      const list = map.get(key) ?? [];
      list.push(event);
      map.set(key, list);
    }
    return Array.from(map.entries()).map(([decade, events]) => ({ decade, events }) satisfies DecadeSection);
  }, [paged.items]);

  if (paged.loading && paged.items.length === 0) {
    return <LoadingBlock label="Loading timeline…" />;
  }

  if (paged.error && paged.items.length === 0) {
    return <ErrorBlock message={paged.error} onRetry={paged.reload} />;
  }

  return (
    <FlatList
      style={[styles.list, { backgroundColor: c.surfacePage }]}
      data={sections}
      keyExtractor={(section) => section.decade}
      ListEmptyComponent={<EmptyBlock message="No timeline events yet." />}
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
      renderItem={({ item: section }) => (
        <View>
          <Text
            style={[
              type.eyebrow,
              {
                color: c.accentArchive,
                paddingHorizontal: space.xl,
                paddingTop: space.xxl,
                paddingBottom: space.md,
              },
            ]}
          >
            {section.decade}
          </Text>
          {section.events.map((event) => {
            const expanded = expandedId === event.id;
            return (
              <Pressable
                key={event.id}
                accessibilityRole="button"
                accessibilityState={{ expanded }}
                accessibilityLabel={event.title}
                onPress={() => setExpandedId(expanded ? null : event.id)}
                style={({ pressed }) => [
                  styles.row,
                  { borderTopColor: c.hairline, opacity: pressed ? 0.72 : 1 },
                ]}
              >
                <Text style={[type.meta, { color: c.textMuted }]}>{event.formattedDate}</Text>
                <Text style={[type.listTitle, { color: c.textPrimary, marginTop: space.xs }]}>
                  {event.title}
                </Text>
                <Text style={[type.meta, { color: c.accentArchive, marginTop: space.xs }]}>
                  {event.categoryLabel}
                </Text>
                {expanded ? (
                  <View style={styles.detail}>
                    <Text style={[type.body, { color: c.textSecondary }]}>{event.summary}</Text>
                    {event.sourceUrl ? (
                      <Pressable
                        accessibilityRole="link"
                        onPress={() => Linking.openURL(event.sourceUrl!)}
                        style={styles.source}
                      >
                        <Text style={[type.button, { color: c.accentPrimary }]}>Source</Text>
                      </Pressable>
                    ) : null}
                  </View>
                ) : null}
              </Pressable>
            );
          })}
        </View>
      )}
    />
  );
}

const styles = StyleSheet.create({
  list: { flex: 1 },
  row: {
    paddingVertical: space.base,
    paddingHorizontal: space.xl,
    borderTopWidth: StyleSheet.hairlineWidth,
  },
  detail: {
    marginTop: space.md,
    gap: space.md,
  },
  source: {
    minHeight: 44,
    justifyContent: 'center',
  },
});
