import { useCallback, useState } from 'react';
import { FlatList, Pressable, RefreshControl, StyleSheet, Text, View } from 'react-native';
import { fetchFreddieTributePage, type FreddieTribute } from '../../api';
import { usePagedContent } from '../../hooks/usePagedContent';
import { EmptyBlock, ErrorBlock, ListFooterLoading, LoadingBlock } from '../../ui/ScreenStates';
import { space, type, useTheme } from '../../theme';

function metaLine(item: FreddieTribute): string {
  return [item.country, item.dateText, item.timeText].filter(Boolean).join(' · ');
}

export function FreddieTributeScreen() {
  const { c } = useTheme();
  const [expandedId, setExpandedId] = useState<number | null>(null);
  const paged = usePagedContent<FreddieTribute>(
    useCallback((page, signal) => fetchFreddieTributePage({ page, pageSize: 20, signal }), []),
  );

  if (paged.loading && paged.items.length === 0) {
    return <LoadingBlock label="Loading tributes…" />;
  }

  if (paged.error && paged.items.length === 0) {
    return <ErrorBlock message={paged.error} onRetry={paged.reload} />;
  }

  return (
    <FlatList
      style={[styles.list, { backgroundColor: c.surfacePage }]}
      data={paged.items}
      keyExtractor={(item) => String(item.id)}
      ListEmptyComponent={<EmptyBlock message="No tributes yet." />}
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
        const expanded = expandedId === item.id;
        const meta = metaLine(item);
        return (
          <Pressable
            accessibilityRole="button"
            accessibilityState={{ expanded }}
            accessibilityLabel={`Tribute from ${item.name}`}
            onPress={() => setExpandedId(expanded ? null : item.id)}
            style={({ pressed }) => [
              styles.row,
              { borderTopColor: c.hairline, opacity: pressed ? 0.72 : 1 },
            ]}
          >
            <Text style={[type.listTitle, { color: c.textPrimary }]}>{item.name}</Text>
            {meta ? (
              <Text style={[type.meta, { color: c.textMuted, marginTop: space.xs }]}>{meta}</Text>
            ) : null}
            <Text
              style={[type.body, { color: c.textSecondary, marginTop: space.sm }]}
              numberOfLines={expanded ? undefined : 3}
            >
              {item.thought}
            </Text>
            {!expanded && item.thought.length > 160 ? (
              <Text style={[type.button, { color: c.accentPrimary, marginTop: space.sm }]}>
                Read more
              </Text>
            ) : null}
          </Pressable>
        );
      }}
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
});
