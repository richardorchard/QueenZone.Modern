import { writeCachedLocalFile, cacheFileName } from '../../media/writeCachedFile';
import { saveLocalFileToPhotos, saveToPhotosCopy } from '../../media/saveToPhotos';
import { isPhotoCdnUrl, photoCdnSource } from './photoGalleryMeta';

export const saveGalleryPhotoCopy = {
  refused: 'This photograph cannot be saved.',
  failed: saveToPhotosCopy.failed,
} as const;

/**
 * Public CDN GET (no Authorization) → cache file → add-only Photos.
 * Refuses non-`cdn.queenzone.org` URLs the same way the viewer does.
 */
export async function saveGalleryPhoto(imageUrl: string, signal?: AbortSignal): Promise<void> {
  const source = photoCdnSource(imageUrl);
  if (!source) {
    throw new Error(saveGalleryPhotoCopy.refused);
  }

  const response = await fetch(source.uri, {
    method: 'GET',
    redirect: 'follow',
    credentials: 'omit',
    headers: {
      Accept: 'image/*',
    },
    signal,
  });

  const finalUrl = response.url?.trim() || source.uri;
  if (!response.ok || !isPhotoCdnUrl(finalUrl)) {
    throw new Error(response.ok ? saveGalleryPhotoCopy.refused : saveGalleryPhotoCopy.failed);
  }

  const bytes = new Uint8Array(await response.arrayBuffer());
  const fileUri = await writeCachedLocalFile(galleryCacheFileName(source.uri), bytes);
  await saveLocalFileToPhotos(fileUri);
}

export function galleryCacheFileName(imageUrl: string): string {
  try {
    return cacheFileName(new URL(imageUrl).pathname);
  } catch {
    return cacheFileName('photograph.jpg');
  }
}
