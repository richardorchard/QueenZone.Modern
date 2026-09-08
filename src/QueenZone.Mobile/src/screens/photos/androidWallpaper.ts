import { Platform } from 'react-native';
import {
  wallpaperCopy,
  wallpaperFailureMessage,
  type WallpaperTarget,
} from './wallpaperMeta';

export async function setAndroidWallpaperFromFile(
  fileUri: string,
  target: WallpaperTarget,
): Promise<void> {
  if (Platform.OS !== 'android') {
    throw new Error(wallpaperCopy.androidOnly);
  }

  // Lazy so iOS never loads WallpaperManager / requireNativeModule.
  // eslint-disable-next-line @typescript-eslint/no-require-imports -- guarded Android-only native load.
  const { setWallpaper } = require('../../../modules/queenzone-wallpaper') as {
    setWallpaper: (uri: string, wallpaperTarget: WallpaperTarget) => Promise<void>;
  };

  try {
    await setWallpaper(fileUri, target);
  } catch (err: unknown) {
    const code = err instanceof Error ? err.message : '';
    throw new Error(wallpaperFailureMessage(target, code));
  }
}
