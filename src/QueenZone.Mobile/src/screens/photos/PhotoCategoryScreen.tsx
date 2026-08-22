import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { useCallback, useEffect, useLayoutEffect, useState } from 'react';
import {
  FlatList,
  Pressable,
  RefreshControl,
  ScrollView,
  Text,
  useWindowDimensions,
  View,
} from 'react-native';
import {
  ApiError,
  fetchPhotoCategory,
  fetchPhotoCategoryItems,
  type PhotoCategoryListItem,
  type PhotoListItem,
} from '../../api';
import { usePagedContent } from '../../hooks/usePagedContent';
import type { PhotosStackParamList } from '../../navigation/types';
import { space, type, useTheme } from '../../theme';
import { ArchiveImage } from '../../ui/ArchiveImage';
import { Chip } from '../../ui/Chip';
import { EmptyBlock, ErrorBlock, ListFooterLoading, LoadingBlock } from '../../ui/ScreenStates';
import {
  photoCategoryPageSize,
  photoCdnSource,
  photoCountLabel,
  photoRangeLabel,
  photoSizePresets,
  photoThumbMeta,
  photoViewerParams,
  type PhotoSizeQuery,
} from './photoGalleryMeta';

type Props = NativeStackScreenProps<PhotosStackParamList, 'PhotoCategory'>;

const GAP = 3;
const COLS = 3;

function messageFromUnknownError(err: unknown): string {
  return err instanceof ApiError ? err.message : 'Something went wrong.';
}

export function PhotoCategoryScreen({ navigation, route }: Props) {
  const { c } = useTheme();
  const { slug, name } = route.params;
  const width = useWindowDimensions().width;
  const tile = (width - GAP * (COLS - 1) - GAP * 2) / COLS;
  const [size, setSize] = useState<PhotoSizeQuery>('');
  const [category, setCategory] = useState<PhotoCategoryListItem | null>(null);
  const [categoryError, setCategoryError] = useState<string | null>(null);
  const [categoryReloadToken, setCategoryReloadToken] = useState(0);

  const paged = usePagedContent<PhotoListItem>(
    useCallback(
      (page, signal) =>
        fetchPhotoCategoryItems(slug, {
          page,
          pageSize: photoCategoryPageSize,
          size: size || undefined,
          signal,
        }),
      [slug, size],
    ),
    photoCategoryPageSize,
    size,
  );

  useEffect(() => {
    const controller = new AbortController();
    setCategoryError(null);
    fetchPhotoCategory(slug, controller.signal)
      .then((item) => {
        setCategory(item);
        setCategoryError(null);
      })
      .catch((err: unknown) => {
        if (err instanceof Error && err.name === 'AbortError') {
          return;
        }
        setCategory(null);
        setCategoryError(messageFromUnknownError(err));
      });
    return () => controller.abort();
  }, [slug, categoryReloadToken]);

  useLayoutEffect(() => {
    navigation.setOptions({ title: category?.name ?? name ?? 'Collection' });
  }, [category?.name, name, navigation]);

  const retry = useCallback(() => {
    setCategoryReloadToken((n) => n + 1);
    paged.reload();
  }, [paged]);

  if ((paged.loading && paged.items.length === 0) || (!category && !categoryError && !paged.error)) {
    return <LoadingBlock label="Loading collection…" />;
  }

  if ((categoryError || paged.error) && paged.items.length === 0 && !category) {
    return <ErrorBlock message={categoryError ?? paged.error ?? 'Collection not found.'} onRetry={retry} />;
  }

  return (
    <FlatList
      style={{ flex: 1, backgroundColor: c.surfacePage }}
      data={paged.items}
      keyExtractor={(item) => String(item.picId)}
      numColumns={COLS}
      ListHeaderComponent={
        <View>
          <View style={{ paddingHorizontal: space.xl, paddingTop: space.base, paddingBottom: space.sm }}>
            <Text style={[type.caption, { color: c.textSecondary }]}>
              {category
                ? `${photoCountLabel(category.imageCount)} in the archive`
                : 'Photography'}
            </Text>
            {paged.totalCount > 0 ? (
              <Text style={[type.meta, { color: c.textMuted, marginTop: space.xs }]}>
                {photoRangeLabel(1, photoCategoryPageSize, paged.totalCount, paged.items.length)}
              </Text>
            ) : null}
          </View>
          <ScrollView
            horizontal
            showsHorizontalScrollIndicator={false}
            contentContainerStyle={{ paddingHorizontal: space.xl, gap: 8, paddingBottom: 16 }}
          >
            {photoSizePresets.map((preset) => (
              <Chip
                key={preset.query || 'all'}
                label={preset.label}
                active={size === preset.query}
                onPress={() => setSize(preset.query)}
              />
            ))}
          </ScrollView>
        </View>
      }
      columnWrapperStyle={{ gap: GAP, paddingHorizontal: GAP }}
      contentContainerStyle={{ paddingBottom: space.section }}
      ListEmptyComponent={
        <EmptyBlock
          message={
            size
              ? `No images match ${photoSizePresets.find((preset) => preset.query === size)?.label ?? 'this filter'}.`
              : 'No images are available in this collection yet.'
          }
        />
      }
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
      windowSize={7}
      maxToRenderPerBatch={12}
      initialNumToRender={12}
      removeClippedSubviews
      renderItem={({ item }) => {
        const thumb = photoCdnSource(item.thumbnailUrl);
        return (
          <Pressable
            accessibilityRole="button"
            accessibilityLabel={item.title}
            onPress={() =>
              navigation.navigate('PhotoViewer', photoViewerParams(item.categorySlug, item.picId, size))
            }
            style={{ width: tile, marginBottom: GAP }}
          >
            {thumb ? (
              <ArchiveImage
                source={thumb}
                label={item.title}
                recyclingKey={`photo-${item.picId}`}
                priority="low"
                style={{ width: tile, height: tile }}
              />
            ) : (
              <View style={{ width: tile, height: tile, backgroundColor: c.surfaceCard }} />
            )}
            <Text
              style={[type.meta, { color: c.textPrimary, marginTop: 4 }]}
              numberOfLines={1}
            >
              {item.title}
            </Text>
            <Text style={[type.meta, { color: c.textMuted }]} numberOfLines={1}>
              {photoThumbMeta(item)}
            </Text>
          </Pressable>
        );
      }}
    />
  );
}
