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
