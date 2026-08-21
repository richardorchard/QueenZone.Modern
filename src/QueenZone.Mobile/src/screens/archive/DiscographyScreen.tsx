import { useCallback } from 'react';
import { FlatList, Image, Pressable, RefreshControl, StyleSheet, Text, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { fetchDiscographyPage, type AlbumListItem } from '../../api';
import { usePagedContent } from '../../hooks/usePagedContent';
import type { ArchiveStackParamList } from '../../navigation/types';
import { EmptyBlock, ErrorBlock, ListFooterLoading, LoadingBlock } from '../../ui/ScreenStates';
import { radius, space, type, useTheme } from '../../theme';

type Props = NativeStackScreenProps<ArchiveStackParamList, 'Discography'>;

export function DiscographyScreen({ navigation }: Props) {
  const { c } = useTheme();
  const paged = usePagedContent<AlbumListItem>(
    useCallback((page, signal) => fetchDiscographyPage({ page, pageSize: 50, signal }), []),
    50,
  );

  if (paged.loading && paged.items.length === 0) {
    return <LoadingBlock label="Loading discography…" />;
  }

  if (paged.error && paged.items.length === 0) {
    return <ErrorBlock message={paged.error} onRetry={paged.reload} />;
  }

  return (
    <FlatList
      style={[styles.list, { backgroundColor: c.surfacePage }]}
      data={paged.items}
      keyExtractor={(item) => String(item.albumId)}
      ListEmptyComponent={<EmptyBlock message="No albums yet." />}
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
        <Pressable
          accessibilityRole="button"
          accessibilityLabel={`Open album ${item.name}`}
          onPress={() => navigation.navigate('Album', { id: item.albumId })}
          style={({ pressed }) => [styles.row, { borderTopColor: c.hairline, opacity: pressed ? 0.72 : 1 }]}
        >
          {item.thumbnailUrl ? (
            <Image source={{ uri: item.thumbnailUrl }} style={styles.thumb} accessibilityIgnoresInvertColors />
          ) : (
            <View style={[styles.thumb, { backgroundColor: c.surfaceCard, borderColor: c.hairline, borderWidth: 1 }]} />
          )}
          <View style={styles.text}>
            <Text style={[type.listTitle, { color: c.textPrimary }]} numberOfLines={2}>
              {item.name}
            </Text>
            {item.releaseYear != null ? (
              <Text style={[type.meta, { color: c.textMuted, marginTop: space.xs }]}>
                {item.releaseYear}
              </Text>
            ) : null}
          </View>
        </Pressable>
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
    paddingVertical: space.base,
    paddingHorizontal: space.xl,
    borderTopWidth: StyleSheet.hairlineWidth,
  },
  thumb: {
    width: 64,
    height: 64,
    borderRadius: radius.xs,
  },
  text: {
    flex: 1,
  },
});
