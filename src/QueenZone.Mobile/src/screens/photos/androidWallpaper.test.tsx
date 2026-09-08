import { Platform } from 'react-native';
import { setWallpaper } from '../../../modules/queenzone-wallpaper';
import { setAndroidWallpaperFromFile } from './androidWallpaper';
import { wallpaperCopy } from './wallpaperMeta';

jest.mock('../../../modules/queenzone-wallpaper', () => ({
  setWallpaper: jest.fn(),
}));

const setNativeWallpaper = setWallpaper as jest.MockedFunction<typeof setWallpaper>;

describe('setAndroidWallpaperFromFile', () => {
  const originalOs = Platform.OS;

  afterEach(() => {
    Object.defineProperty(Platform, 'OS', { configurable: true, value: originalOs });
  });

  it('does not load WallpaperManager or claim success on iOS', async () => {
    Object.defineProperty(Platform, 'OS', { configurable: true, value: 'ios' });

    await expect(setAndroidWallpaperFromFile('file:///cache/img-101.jpg', 'home')).rejects.toMatchObject({
      message: wallpaperCopy.androidOnly,
    });
    expect(setNativeWallpaper).not.toHaveBeenCalled();
  });

  it('sets the chosen Android target through WallpaperManager', async () => {
    Object.defineProperty(Platform, 'OS', { configurable: true, value: 'android' });
    setNativeWallpaper.mockResolvedValueOnce(undefined);

    await setAndroidWallpaperFromFile('file:///cache/img-101.jpg', 'lock');

    expect(setNativeWallpaper).toHaveBeenCalledWith('file:///cache/img-101.jpg', 'lock');
  });

  it('maps a lock-target native failure to a lock-screen error', async () => {
    Object.defineProperty(Platform, 'OS', { configurable: true, value: 'android' });
    setNativeWallpaper.mockRejectedValueOnce(new Error('target-failed'));

    await expect(setAndroidWallpaperFromFile('file:///cache/img-101.jpg', 'lock')).rejects.toMatchObject({
      message: wallpaperCopy.lockFailed,
    });
  });
});
