import { memo, useCallback, useState } from 'react';
import { Pressable, StyleSheet, Text, type ListRenderItem } from 'react-native';
import { fetchFreddieTributePage, type FreddieTribute } from '../../api';
import { usePagedContent } from '../../hooks/usePagedContent';
import { PagedListScreen } from '../../ui/PagedListScreen';
import { space, type, useTheme } from '../../theme';

function metaLine(item: FreddieTribute): string {
  return [item.country, item.dateText, item.timeText].filter(Boolean).join(' · ');
}

function tributeKeyExtractor(item: FreddieTribute): string {
  return String(item.id);
}

const FreddieTributeRow = memo(function FreddieTributeRow({
  item,
  expanded,
  onToggle,
}: {
  item: FreddieTribute;
  expanded: boolean;
  onToggle: (id: number) => void;
}) {
  const { c } = useTheme();
  const meta = metaLine(item);
  return (
    <Pressable
      accessibilityRole="button"
      accessibilityState={{ expanded }}
      accessibilityLabel={`Tribute from ${item.name}`}
      onPress={() => onToggle(item.id)}
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
});

export function FreddieTributeScreen() {
  const [expandedId, setExpandedId] = useState<number | null>(null);
  const paged = usePagedContent<FreddieTribute>(
    useCallback((page, signal) => fetchFreddieTributePage({ page, pageSize: 20, signal }), []),
  );

  const toggleExpanded = useCallback((id: number) => {
    setExpandedId((current) => (current === id ? null : id));
  }, []);

  const renderItem = useCallback<ListRenderItem<FreddieTribute>>(
    ({ item }) => (
      <FreddieTributeRow
        item={item}
        expanded={expandedId === item.id}
        onToggle={toggleExpanded}
      />
    ),
    [expandedId, toggleExpanded],
  );

  return (
    <PagedListScreen
      paged={paged}
      keyExtractor={tributeKeyExtractor}
      loadingLabel="Loading tributes…"
      emptyMessage="No tributes yet."
      renderItem={renderItem}
    />
  );
}

const styles = StyleSheet.create({
  row: {
    paddingVertical: space.base,
    paddingHorizontal: space.xl,
    borderTopWidth: StyleSheet.hairlineWidth,
  },
});
