import { cacheGalleryPhoto } from './saveGalleryPhoto';
import { setAndroidWallpaperFromFile } from './androidWallpaper';
import { wallpaperFailureMessage, type WallpaperTarget } from './wallpaperMeta';

export async function setAndroidGalleryWallpaper(
  imageUrl: string,
  target: WallpaperTarget,
  signal?: AbortSignal,
): Promise<void> {
  const fileUri = await cacheGalleryPhoto(imageUrl, signal);
  try {
    await setAndroidWallpaperFromFile(fileUri, target);
  } catch (err: unknown) {
    if (err instanceof Error && err.message.length > 0) {
      throw err;
    }
    throw new Error(wallpaperFailureMessage(target));
  }
}
