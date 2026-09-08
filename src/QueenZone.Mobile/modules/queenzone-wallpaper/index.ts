import { requireNativeModule } from 'expo';

type NativeWallpaper = {
  setWallpaper(fileUri: string, target: string): Promise<void>;
};

/** Android WallpaperManager. Call only after a Platform.OS === 'android' guard. */
export async function setWallpaper(fileUri: string, target: string): Promise<void> {
  const native = requireNativeModule<NativeWallpaper>('QueenZoneWallpaper');
  await native.setWallpaper(fileUri, target);
}
