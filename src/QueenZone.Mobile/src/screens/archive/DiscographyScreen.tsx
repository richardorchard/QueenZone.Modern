import { useCallback } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { fetchDiscographyPage, type AlbumListItem } from '../../api';
import { usePagedContent } from '../../hooks/usePagedContent';
import type { ArchiveStackParamList } from '../../navigation/types';
import { ArchiveImage } from '../../ui/ArchiveImage';
import { PagedListScreen } from '../../ui/PagedListScreen';
import { radius, space, type, useTheme } from '../../theme';

type Props = NativeStackScreenProps<ArchiveStackParamList, 'Discography'>;

export function DiscographyScreen({ navigation }: Props) {
  const { c } = useTheme();
  const paged = usePagedContent<AlbumListItem>(
    useCallback((page, signal) => fetchDiscographyPage({ page, pageSize: 20, signal }), []),
  );

  return (
    <PagedListScreen
      paged={paged}
      keyExtractor={(item) => String(item.albumId)}
      loadingLabel="Loading discography…"
      emptyMessage="No albums yet."
      renderItem={({ item }) => (
        <Pressable
          accessibilityRole="button"
          accessibilityLabel={`Open album ${item.name}`}
          onPress={() => navigation.navigate('Album', { id: item.albumId })}
          style={({ pressed }) => [styles.row, { borderTopColor: c.hairline, opacity: pressed ? 0.72 : 1 }]}
        >
          {item.thumbnailUrl ? (
            <ArchiveImage
              source={{ uri: item.thumbnailUrl }}
              style={styles.thumb}
              priority="low"
              recyclingKey={String(item.albumId)}
              label={item.name}
              accessibilityIgnoresInvertColors
            />
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
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: space.base,
    paddingVertical: space.base,
    paddingHorizontal: space.xl,
    borderTopWidth: StyleSheet.hairlineWidth,
  },
  thumb: {
    width: space.thumb,
    height: space.thumb,
    borderRadius: radius.xs,
  },
  text: {
    flex: 1,
  },
});
