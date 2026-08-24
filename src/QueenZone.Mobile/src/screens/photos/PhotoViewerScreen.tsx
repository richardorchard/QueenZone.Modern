import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import { ChevronLeft, ChevronRight, X } from 'lucide-react-native';
import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { PanResponder, StyleSheet, Text, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { ApiError, fetchPhotoDetail, type PhotoDetail } from '../../api';
import type { PhotosStackParamList } from '../../navigation/types';
import { testIds } from '../../test/testIds';
import { type, useTheme } from '../../theme';
import { ArchiveImage } from '../../ui/ArchiveImage';
import { IconButton } from '../../ui/IconButton';
import { MetaLine } from '../../ui/MetaLine';
import { ErrorBlock, LoadingBlock } from '../../ui/ScreenStates';
import {
  photoCdnSource,
  photoCounterLabel,
  photoDetailMeta,
  photoSizeFromPath,
  photoSwipeDirection,
  photoSwipeIsTap,
  photoSwipeShouldCapture,
  photoSwipeShouldStart,
  photoViewerParams,
  resolvedPhotoSize,
} from './photoGalleryMeta';

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

  const swipeResponder = useMemo(
    () =>
      PanResponder.create({
        onStartShouldSetPanResponder: (event) => photoSwipeShouldStart(event.nativeEvent.pageX),
        onMoveShouldSetPanResponder: (event, gesture) =>
          photoSwipeShouldCapture(gesture.dx, gesture.dy, event.nativeEvent.pageX - gesture.dx),
        onMoveShouldSetPanResponderCapture: (event, gesture) =>
          photoSwipeShouldCapture(gesture.dx, gesture.dy, event.nativeEvent.pageX - gesture.dx),
        onPanResponderTerminationRequest: () => false,
        onPanResponderRelease: (_, gesture) => {
          const direction = photoSwipeDirection(gesture.dx, gesture.dy);
          if (direction === 'previous' && previousPicId != null) {
            goTo(previousPicId);
            return;
          }
          if (direction === 'next' && nextPicId != null) {
            goTo(nextPicId);
            return;
          }
          if (photoSwipeIsTap(gesture.dx, gesture.dy)) {
            setChromeVisible((value) => !value);
          }
        },
      }),
    [goTo, nextPicId, previousPicId],
  );

  if (loading && !photo) {
    return <LoadingBlock label="Loading photograph…" />;
  }

  if (error || !photo) {
    return <ErrorBlock message={error ?? 'Photograph not found.'} onRetry={retry} />;
  }

  const image = photoCdnSource(photo.imageUrl);

  return (
    <View testID={testIds.photoViewerScreen} style={{ flex: 1, backgroundColor: '#000' }}>
      <View
        style={{ flex: 1 }}
        collapsable={false}
        accessibilityHint="Swipe left or right to change photograph"
        {...swipeResponder.panHandlers}
      >
        {image ? (
          <View pointerEvents="none" style={{ flex: 1, width: '100%' }}>
            <ArchiveImage
              source={image}
              label={photo.title}
              contentFit="contain"
              recyclingKey={`photo-full-${photo.picId}`}
              priority="high"
              style={{ flex: 1, width: '100%' }}
            />
          </View>
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
