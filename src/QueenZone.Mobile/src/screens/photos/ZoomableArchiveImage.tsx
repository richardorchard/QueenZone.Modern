import { useCallback, useEffect, useMemo } from 'react';
import { AccessibilityInfo, type LayoutChangeEvent, StyleSheet, View } from 'react-native';
import { Gesture, GestureDetector } from 'react-native-gesture-handler';
import Animated, {
  runOnJS,
  useAnimatedStyle,
  useSharedValue,
  withSpring,
} from 'react-native-reanimated';
import {
  photoSwipeDirection,
  photoSwipeEdgeGuardPx,
  photoSwipeIsTap,
  photoSwipeShouldStart,
  photoSwipeCapturePx,
  type PhotoSwipeDirection,
} from './photoGalleryMeta';
import {
  clampPhotoPanTranslation,
  clampPhotoZoomScale,
  focalPhotoZoomTranslation,
  isPhotoZoomed,
  photoPanShouldActivate,
  photoZoomAccessibilityMessage,
  photoZoomDoubleTapScale,
  photoZoomMinScale,
  photoZoomSpringConfig,
} from './photoZoomMeta';
import { ArchiveImage } from '../../ui/ArchiveImage';

type Props = {
  source: { uri: string };
  label: string;
  recyclingKey: string;
  imageWidth: number;
  imageHeight: number;
  /** Resets zoom/pan when the displayed photograph changes. */
  resetKey: number | string;
  canSwipePrevious: boolean;
  canSwipeNext: boolean;
  onGallerySwipe: (direction: PhotoSwipeDirection) => void;
  onToggleChrome: () => void;
};

export function ZoomableArchiveImage({
  source,
  label,
  recyclingKey,
  imageWidth,
  imageHeight,
  resetKey,
  canSwipePrevious,
  canSwipeNext,
  onGallerySwipe,
  onToggleChrome,
}: Props) {
  const scale = useSharedValue(photoZoomMinScale);
  const savedScale = useSharedValue(photoZoomMinScale);
  const translateX = useSharedValue(0);
  const translateY = useSharedValue(0);
  const savedTranslateX = useSharedValue(0);
  const savedTranslateY = useSharedValue(0);
  const containerWidth = useSharedValue(0);
  const containerHeight = useSharedValue(0);
  const pinchStartScale = useSharedValue(photoZoomMinScale);
  const pinchStartTranslateX = useSharedValue(0);
  const pinchStartTranslateY = useSharedValue(0);
  const gestureStartPageX = useSharedValue(0);
  const gestureStartX = useSharedValue(0);
  const gestureStartY = useSharedValue(0);
  const imageWidthValue = useSharedValue(imageWidth);
  const imageHeightValue = useSharedValue(imageHeight);

  useEffect(() => {
    imageWidthValue.value = imageWidth;
    imageHeightValue.value = imageHeight;
  }, [imageHeight, imageHeightValue, imageWidth, imageWidthValue]);

  useEffect(() => {
    scale.value = photoZoomMinScale;
    savedScale.value = photoZoomMinScale;
    translateX.value = 0;
    translateY.value = 0;
    savedTranslateX.value = 0;
    savedTranslateY.value = 0;
  }, [resetKey]);

  const announceZoom = useCallback((nextScale: number) => {
    AccessibilityInfo.announceForAccessibility(photoZoomAccessibilityMessage(nextScale));
  }, []);

  const handlePanEnd = useCallback(
    (dx: number, dy: number, startPageX: number) => {
      if (!photoSwipeShouldStart(startPageX)) {
        return;
      }

      const direction = photoSwipeDirection(dx, dy);
      if (direction === 'previous' && canSwipePrevious) {
        onGallerySwipe('previous');
        return;
      }
      if (direction === 'next' && canSwipeNext) {
        onGallerySwipe('next');
        return;
      }
      if (photoSwipeIsTap(dx, dy)) {
        onToggleChrome();
      }
    },
    [canSwipeNext, canSwipePrevious, onGallerySwipe, onToggleChrome],
  );

  const onLayout = useCallback(
    (event: LayoutChangeEvent) => {
      containerWidth.value = event.nativeEvent.layout.width;
      containerHeight.value = event.nativeEvent.layout.height;
    },
    [containerHeight, containerWidth],
  );

  const composedGesture = useMemo(() => {
    const resetZoomAnimated = (announce: boolean) => {
      'worklet';
      scale.value = withSpring(photoZoomMinScale, photoZoomSpringConfig);
      savedScale.value = photoZoomMinScale;
      translateX.value = withSpring(0, photoZoomSpringConfig);
      translateY.value = withSpring(0, photoZoomSpringConfig);
      savedTranslateX.value = 0;
      savedTranslateY.value = 0;
      if (announce) {
        runOnJS(announceZoom)(photoZoomMinScale);
      }
    };

    const containerSize = () => ({
      width: containerWidth.value,
      height: containerHeight.value,
    });

    const imageSize = () => ({
      width: imageWidthValue.value,
      height: imageHeightValue.value,
    });

    const applyPan = (nextX: number, nextY: number, currentScale: number) => {
      'worklet';
      const clamped = clampPhotoPanTranslation(
        nextX,
        nextY,
        currentScale,
        containerSize(),
        imageSize(),
      );
      translateX.value = clamped.x;
      translateY.value = clamped.y;
    };

    const pinchGesture = Gesture.Pinch()
      .onBegin(() => {
        pinchStartScale.value = scale.value;
        pinchStartTranslateX.value = translateX.value;
        pinchStartTranslateY.value = translateY.value;
      })
      .onUpdate((event) => {
        const newScale = clampPhotoZoomScale(pinchStartScale.value * event.scale);
        const focal = focalPhotoZoomTranslation(
          pinchStartTranslateX.value,
          pinchStartTranslateY.value,
          pinchStartScale.value,
          newScale,
          event.focalX,
          event.focalY,
          containerSize(),
        );
        scale.value = newScale;
        applyPan(focal.x, focal.y, newScale);
      })
      .onEnd(() => {
        if (!isPhotoZoomed(scale.value)) {
          resetZoomAnimated(true);
          return;
        }

        savedScale.value = scale.value;
        savedTranslateX.value = translateX.value;
        savedTranslateY.value = translateY.value;
        runOnJS(announceZoom)(scale.value);
      });

    const panGesture = Gesture.Pan()
      .manualActivation(true)
      .onTouchesDown((event) => {
        const touch = event.allTouches[0];
        if (!touch) {
          return;
        }

        gestureStartPageX.value = touch.absoluteX;
        gestureStartX.value = touch.x;
        gestureStartY.value = touch.y;
      })
      .onTouchesMove((event, state) => {
        const touch = event.allTouches[0];
        if (!touch) {
          return;
        }

        const dx = touch.x - gestureStartX.value;
        const dy = touch.y - gestureStartY.value;
        if (
          photoPanShouldActivate(
            dx,
            dy,
            gestureStartPageX.value,
            isPhotoZoomed(scale.value),
            photoSwipeCapturePx,
            photoSwipeEdgeGuardPx,
          )
        ) {
          state.activate();
        }
      })
      .onUpdate((event) => {
        if (!isPhotoZoomed(scale.value)) {
          return;
        }

        applyPan(
          savedTranslateX.value + event.translationX,
          savedTranslateY.value + event.translationY,
          scale.value,
        );
      })
      .onEnd((event) => {
        if (isPhotoZoomed(scale.value)) {
          savedTranslateX.value = translateX.value;
          savedTranslateY.value = translateY.value;
          return;
        }

        runOnJS(handlePanEnd)(
          event.translationX,
          event.translationY,
          event.absoluteX - event.translationX,
        );
      });

    const doubleTapGesture = Gesture.Tap()
      .numberOfTaps(2)
      .maxDuration(250)
      .onEnd((event) => {
        if (isPhotoZoomed(scale.value)) {
          resetZoomAnimated(true);
          return;
        }

        const newScale = photoZoomDoubleTapScale;
        const focal = focalPhotoZoomTranslation(
          0,
          0,
          photoZoomMinScale,
          newScale,
          event.x,
          event.y,
          containerSize(),
        );
        const clamped = clampPhotoPanTranslation(
          focal.x,
          focal.y,
          newScale,
          containerSize(),
          imageSize(),
        );
        scale.value = withSpring(newScale, photoZoomSpringConfig);
        savedScale.value = newScale;
        translateX.value = withSpring(clamped.x, photoZoomSpringConfig);
        translateY.value = withSpring(clamped.y, photoZoomSpringConfig);
        savedTranslateX.value = clamped.x;
        savedTranslateY.value = clamped.y;
        runOnJS(announceZoom)(newScale);
      });

    return Gesture.Simultaneous(pinchGesture, Gesture.Exclusive(doubleTapGesture, panGesture));
  }, [announceZoom, handlePanEnd]);

  const animatedStyle = useAnimatedStyle(() => ({
    flex: 1,
    width: '100%',
    transform: [
      { translateX: translateX.value },
      { translateY: translateY.value },
      { scale: scale.value },
    ],
  }));

  return (
    <GestureDetector gesture={composedGesture}>
      <View
        style={styles.container}
        collapsable={false}
        onLayout={onLayout}
        accessibilityHint="Pinch or double tap to zoom. Swipe left or right to change photograph."
      >
        <Animated.View style={animatedStyle}>
          <ArchiveImage
            source={source}
            label={label}
            contentFit="contain"
            recyclingKey={recyclingKey}
            priority="high"
            style={styles.image}
          />
        </Animated.View>
      </View>
    </GestureDetector>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    width: '100%',
    overflow: 'hidden',
  },
  image: {
    flex: 1,
    width: '100%',
  },
});
