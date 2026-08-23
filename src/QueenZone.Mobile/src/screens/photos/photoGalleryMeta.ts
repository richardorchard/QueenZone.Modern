/** Matches website category pages and `/api/v1/content/photos/.../items`. */
export const photoCategoryPageSize = 24;

export const photoCdnOrigin = 'https://cdn.queenzone.org';

export const photoSizePresets = [
  { query: '', label: 'All sizes' },
  { query: 'desktop', label: 'Desktop wallpaper' },
  { query: 'phone', label: 'Phone wallpaper' },
  { query: 'large', label: 'Large (1920+)' },
  { query: 'hd', label: 'HD (1280+)' },
  { query: 'landscape', label: 'Landscape' },
  { query: 'portrait', label: 'Portrait' },
] as const;

export type PhotoSizeQuery = (typeof photoSizePresets)[number]['query'];

export function isPhotoCdnUrl(url: string | null | undefined): boolean {
  const trimmed = url?.trim() ?? '';
  return trimmed.startsWith(`${photoCdnOrigin}/`);
}

export function photoCdnSource(url: string | null | undefined): { uri: string } | null {
  return isPhotoCdnUrl(url) ? { uri: url!.trim() } : null;
}

export function photoCountLabel(count: number): string {
  return `${count.toLocaleString()} ${count === 1 ? 'image' : 'images'}`;
}

export function photoRangeLabel(
  page: number,
  pageSize: number,
  totalCount: number,
  itemCount: number,
): string {
  if (totalCount <= 0 || itemCount <= 0) {
    return 'No images';
  }

  const start = (Math.max(page, 1) - 1) * pageSize + 1;
  const end = start + itemCount - 1;
  return `Showing ${start}–${end} of ${totalCount}`;
}

export function photoThumbMeta(photo: {
  year: number;
  pictureDimensionsLabel: string | null;
}): string {
  const parts = [String(photo.year)];
  if (photo.pictureDimensionsLabel?.trim()) {
    parts.push(photo.pictureDimensionsLabel.trim());
  }
  return parts.join(' · ');
}

export function photoDetailMeta(photo: {
  year: number;
  categoryName: string;
  pictureDimensionsLabel: string | null;
  submittedByDisplayName: string | null;
}): string[] {
  const parts = [String(photo.year), photo.categoryName];
  if (photo.pictureDimensionsLabel?.trim()) {
    parts.push(photo.pictureDimensionsLabel.trim());
  }
  if (photo.submittedByDisplayName?.trim()) {
    parts.push(`Submitted by ${photo.submittedByDisplayName.trim()}`);
  }
  return parts;
}

export function photoCounterLabel(index: number, count: number): string {
  return `${index + 1} / ${count}`;
}

/** Carries website `?size=` through Viewer prev/next. Empty means all sizes. */
export function photoViewerParams(
  slug: string,
  picId: number,
  size?: string | null,
): { slug: string; picId: number; size?: string } {
  const query = size?.trim() ?? '';
  return query.length > 0 ? { slug, picId, size: query } : { slug, picId };
}

/** Reads the website `?size=` query from a detail/category path. */
export function photoSizeFromPath(path: string | null | undefined): string | undefined {
  if (!path) {
    return undefined;
  }

  const queryIndex = path.indexOf('?');
  if (queryIndex < 0) {
    return undefined;
  }

  const value = new URLSearchParams(path.slice(queryIndex + 1)).get('size')?.trim() ?? '';
  return value.length > 0 ? value : undefined;
}

/**
 * If the API dropped the requested filter (fallback / ignored `?size=`),
 * return undefined so chips and prev/next match the unfiltered set.
 */
export function resolvedPhotoSize(
  requested: string | null | undefined,
  path: string | null | undefined,
): string | undefined {
  const sent = requested?.trim() || undefined;
  if (!sent) {
    return undefined;
  }

  return photoSizeFromPath(path);
}

/** Horizontal travel needed before a swipe changes photograph. */
export const photoSwipeThresholdPx = 56;

/** Ignore the gesture when vertical travel is larger than this. */
export const photoSwipeMaxOffAxisPx = 72;

/** Leave the iOS back-edge gesture to the native stack. */
export const photoSwipeEdgeGuardPx = 24;

/** Horizontal travel before the viewer captures the pan from the image. */
export const photoSwipeCapturePx = 16;

/** Movement below this is treated as a tap to toggle viewer chrome. */
export const photoSwipeTapSlopPx = 10;

export type PhotoSwipeDirection = 'previous' | 'next';

/**
 * Maps a drag vector to gallery navigation. Swipe right (positive `dx`) goes
 * to the previous image; swipe left goes to the next — same as the arrows.
 */
export function photoSwipeDirection(
  dx: number,
  dy: number,
  threshold = photoSwipeThresholdPx,
  maxOffAxis = photoSwipeMaxOffAxisPx,
): PhotoSwipeDirection | null {
  if (!Number.isFinite(dx) || !Number.isFinite(dy)) {
    return null;
  }

  if (Math.abs(dx) < threshold || Math.abs(dy) > maxOffAxis || Math.abs(dx) <= Math.abs(dy)) {
    return null;
  }

  return dx > 0 ? 'previous' : 'next';
}

/**
 * Whether the photo viewer should take over a pan. Left-edge starts stay with
 * the native back gesture; otherwise a mostly-horizontal move wins over the image.
 */
export function photoSwipeShouldCapture(
  dx: number,
  dy: number,
  startPageX: number,
  edgeGuardPx = photoSwipeEdgeGuardPx,
  capturePx = photoSwipeCapturePx,
): boolean {
  if (!Number.isFinite(dx) || !Number.isFinite(dy) || !Number.isFinite(startPageX)) {
    return false;
  }

  if (startPageX < edgeGuardPx) {
    return false;
  }

  return Math.abs(dx) > capturePx && Math.abs(dx) > Math.abs(dy);
}

/** Claim the photo surface except along the iOS back-edge. */
export function photoSwipeShouldStart(
  startPageX: number,
  edgeGuardPx = photoSwipeEdgeGuardPx,
): boolean {
  return Number.isFinite(startPageX) && startPageX >= edgeGuardPx;
}

/** A press that did not travel far enough to be a gallery swipe. */
export function photoSwipeIsTap(
  dx: number,
  dy: number,
  slopPx = photoSwipeTapSlopPx,
): boolean {
  if (!Number.isFinite(dx) || !Number.isFinite(dy)) {
    return false;
  }

  return Math.abs(dx) <= slopPx && Math.abs(dy) <= slopPx;
}
