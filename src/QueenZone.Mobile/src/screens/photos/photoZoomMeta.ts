/** Minimum zoom scale (fit to screen). */
export const photoZoomMinScale = 1;

/** Maximum pinch zoom multiplier. */
export const photoZoomMaxScale = 4;

/** Double-tap toggles between 1× and this scale. */
export const photoZoomDoubleTapScale = 2;

/** Spring used when snapping zoom back to fit. */
export const photoZoomSpringConfig = {
  damping: 20,
  stiffness: 200,
};

export type PhotoLayoutSize = {
  width: number;
  height: number;
};

export type PhotoPoint = {
  x: number;
  y: number;
};

export type PhotoPanBounds = {
  maxX: number;
  maxY: number;
};

/** Keeps pinch updates inside the supported zoom range. */
export function clampPhotoZoomScale(
  scale: number,
  min = photoZoomMinScale,
  max = photoZoomMaxScale,
): number {
  'worklet';
  if (!Number.isFinite(scale)) {
    return min;
  }

  return Math.min(max, Math.max(min, scale));
}

/** Whether the viewer is zoomed enough to pan instead of gallery-swipe. */
export function isPhotoZoomed(scale: number, min = photoZoomMinScale): boolean {
  'worklet';
  return scale > min + 0.01;
}

/** Size of a contain-fit image inside the viewer (before extra zoom scale). */
export function containedImageSize(
  container: PhotoLayoutSize,
  image: PhotoLayoutSize,
): PhotoLayoutSize {
  'worklet';
  if (
    container.width <= 0 ||
    container.height <= 0 ||
    image.width <= 0 ||
    image.height <= 0
  ) {
    return { width: 0, height: 0 };
  }

  const imageAspect = image.width / image.height;
  const containerAspect = container.width / container.height;
  if (imageAspect > containerAspect) {
    return { width: container.width, height: container.width / imageAspect };
  }

  return { width: container.height * imageAspect, height: container.height };
}

/** Maximum pan offset so the scaled image still covers the viewport edges. */
export function photoPanBounds(
  scale: number,
  container: PhotoLayoutSize,
  image: PhotoLayoutSize,
): PhotoPanBounds {
  'worklet';
  const contained = containedImageSize(container, image);
  const scaledWidth = contained.width * scale;
  const scaledHeight = contained.height * scale;
  return {
    maxX: Math.max(0, (scaledWidth - container.width) / 2),
    maxY: Math.max(0, (scaledHeight - container.height) / 2),
  };
}

/** Clamps pan translation for the current zoom level. */
export function clampPhotoPanTranslation(
  translateX: number,
  translateY: number,
  scale: number,
  container: PhotoLayoutSize,
  image: PhotoLayoutSize,
): PhotoPoint {
  'worklet';
  const bounds = photoPanBounds(scale, container, image);
  return {
    x: Math.min(bounds.maxX, Math.max(-bounds.maxX, translateX)),
    y: Math.min(bounds.maxY, Math.max(-bounds.maxY, translateY)) || 0,
  };
}

/**
 * Adjusts translation when scale changes so the focal point stays under the
 * user's fingers. Focal coordinates are relative to the viewer view.
 */
export function focalPhotoZoomTranslation(
  translateX: number,
  translateY: number,
  oldScale: number,
  newScale: number,
  focalX: number,
  focalY: number,
  container: PhotoLayoutSize,
): PhotoPoint {
  'worklet';
  if (oldScale <= 0 || !Number.isFinite(oldScale) || container.width <= 0 || container.height <= 0) {
    return { x: translateX, y: translateY };
  }

  const centerX = container.width / 2;
  const centerY = container.height / 2;
  const offsetX = focalX - centerX;
  const offsetY = focalY - centerY;
  const ratio = newScale / oldScale;
  return {
    x: translateX + offsetX * (1 - ratio),
    y: translateY + offsetY * (1 - ratio),
  };
}

/** VoiceOver / TalkBack copy after a zoom change. */
export function photoZoomAccessibilityMessage(scale: number): string {
  if (!Number.isFinite(scale) || scale <= photoZoomMinScale + 0.01) {
    return 'Fit to screen';
  }

  const rounded = Math.round(scale * 10) / 10;
  return `Zoomed to ${rounded} times`;
}

/** Whether a pan gesture should activate (gallery swipe vs idle at 1×). */
export function photoPanShouldActivate(
  dx: number,
  dy: number,
  startPageX: number,
  zoomed: boolean,
  capturePx: number,
  edgeGuardPx: number,
): boolean {
  'worklet';
  if (zoomed) {
    return true;
  }

  if (!Number.isFinite(startPageX) || startPageX < edgeGuardPx) {
    return false;
  }

  return Math.abs(dx) > capturePx && Math.abs(dx) > Math.abs(dy);
}
