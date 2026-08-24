import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { ChevronLeft, ChevronRight, X } from 'lucide-react-native';
import { useCallback, useEffect, useRef, useState } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { ApiError, fetchPhotoDetail, type PhotoDetail } from '../../api';
import type { PhotosStackParamList } from '../../navigation/types';
import { testIds } from '../../test/testIds';
import { type, useTheme } from '../../theme';
import { IconButton } from '../../ui/IconButton';
import { MetaLine } from '../../ui/MetaLine';
import { ErrorBlock, LoadingBlock } from '../../ui/ScreenStates';
import {
  photoCdnSource,
  photoCounterLabel,
  photoDetailMeta,
  photoSizeFromPath,
  photoViewerParams,
  resolvedPhotoSize,
} from './photoGalleryMeta';
import { ZoomableArchiveImage } from './ZoomableArchiveImage';

type Props = NativeStackScreenProps<PhotosStackParamList, 'PhotoViewer'>;

export function PhotoViewerScreen({ navigation, route }: Props) {
  const insets = useSafeAreaInsets();
  const { c } = useTheme();
  const { slug, picId, size } = route.params;
  const [chromeVisible, setChromeVisible] = useState(true);
  const [photo, setPhoto] = useState<PhotoDetail | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [reloadToken, setReloadToken] = useState(0);
  const photoRef = useRef<PhotoDetail | null>(null);
  photoRef.current = photo;

  useEffect(() => {
    const controller = new AbortController();
    setError(null);
    setLoading(photoRef.current == null);
    fetchPhotoDetail(slug, picId, { size, signal: controller.signal })
      .then((detail) => {
        setPhoto(detail);
        setLoading(false);
        if (size && resolvedPhotoSize(size, detail.detailPath) !== size) {
          navigation.setParams({ size: '' });
        }
      })
      .catch((err: unknown) => {
        if (err instanceof Error && err.name === 'AbortError') {
          return;
        }
        setPhoto(null);
        setError(err instanceof ApiError ? err.message : 'Something went wrong.');
        setLoading(false);
      });
    return () => controller.abort();
  }, [slug, picId, size, reloadToken, navigation]);

  const retry = useCallback(() => setReloadToken((n) => n + 1), []);

  const goTo = useCallback(
    (neighborPicId: number) => {
      if (photoRef.current == null || photoRef.current.picId !== picId) {
        return;
      }

      navigation.setParams(
        photoViewerParams(slug, neighborPicId, photoSizeFromPath(photoRef.current.detailPath) ?? size),
      );
    },
    [navigation, picId, size, slug],
  );

  const previousPicId = photo?.previous?.picId ?? null;
  const nextPicId = photo?.next?.picId ?? null;

  const handleGallerySwipe = useCallback(
    (direction: 'previous' | 'next') => {
      if (direction === 'previous' && previousPicId != null) {
        goTo(previousPicId);
        return;
      }
      if (direction === 'next' && nextPicId != null) {
        goTo(nextPicId);
      }
    },
    [goTo, nextPicId, previousPicId],
  );

  const toggleChrome = useCallback(() => {
    setChromeVisible((value) => !value);
  }, []);

  if (loading && !photo) {
    return <LoadingBlock label="Loading photograph…" />;
  }

  if (error || !photo) {
    return <ErrorBlock message={error ?? 'Photograph not found.'} onRetry={retry} />;
  }

  const image = photoCdnSource(photo.imageUrl);

  return (
    <View testID={testIds.photoViewerScreen} style={{ flex: 1, backgroundColor: '#000' }}>
      <View style={{ flex: 1 }}>
        {image ? (
          <ZoomableArchiveImage
            source={image}
            label={photo.title}
            recyclingKey={`photo-full-${photo.picId}`}
            imageWidth={photo.pictureWidth}
            imageHeight={photo.pictureHeight}
            resetKey={photo.picId}
            canSwipePrevious={previousPicId != null}
            canSwipeNext={nextPicId != null}
            onGallerySwipe={handleGallerySwipe}
            onToggleChrome={toggleChrome}
          />
        ) : (
          <View style={{ flex: 1, justifyContent: 'center', alignItems: 'center' }}>
            <Text style={[type.body, { color: c.textSecondary }]}>Image unavailable</Text>
          </View>
        )}
      </View>
      {chromeVisible ? (
        <View pointerEvents="box-none" style={StyleSheet.absoluteFill}>
          <View
            style={{
              position: 'absolute',
              top: insets.top,
              left: 4,
              right: 4,
              flexDirection: 'row',
              alignItems: 'center',
              justifyContent: 'space-between',
            }}
          >
            <IconButton icon={X} accessibilityLabel="Close" onPress={() => navigation.goBack()} />
            <Text style={[type.eyebrow, { color: c.textMuted }]}>
              {photoCounterLabel(photo.index, photo.count)}
            </Text>
            <View style={{ width: 44 }} />
          </View>
          {photo.previous ? (
            <View style={{ position: 'absolute', left: 4, top: '45%' }}>
              <IconButton
                icon={ChevronLeft}
                accessibilityLabel="Previous image"
                onPress={() => goTo(photo.previous!.picId)}
              />
            </View>
          ) : null}
          {photo.next ? (
            <View style={{ position: 'absolute', right: 4, top: '45%' }}>
              <IconButton
                icon={ChevronRight}
                accessibilityLabel="Next image"
                onPress={() => goTo(photo.next!.picId)}
              />
            </View>
          ) : null}
          <View
            style={{
              position: 'absolute',
              left: 24,
              right: 24,
              bottom: insets.bottom + 24,
              gap: 8,
            }}
          >
            <Text style={[type.cardTitle, { color: c.textPrimary }]}>{photo.title}</Text>
            <MetaLine parts={photoDetailMeta(photo)} />
          </View>
        </View>
      ) : null}
    </View>
  );
}
