import { cacheGalleryPhoto } from './saveGalleryPhoto';
import { setAndroidWallpaperFromFile } from './androidWallpaper';
import { setAndroidGalleryWallpaper } from './setGalleryWallpaper';
import { wallpaperCopy } from './wallpaperMeta';

jest.mock('./saveGalleryPhoto', () => {
  const actual = jest.requireActual('./saveGalleryPhoto');
  return {
    ...actual,
    cacheGalleryPhoto: jest.fn(),
  };
});

jest.mock('./androidWallpaper', () => ({
  setAndroidWallpaperFromFile: jest.fn(),
}));

const cachePhoto = cacheGalleryPhoto as jest.MockedFunction<typeof cacheGalleryPhoto>;
const setFromFile = setAndroidWallpaperFromFile as jest.MockedFunction<
  typeof setAndroidWallpaperFromFile
>;

const fullImage = 'https://cdn.queenzone.org/brian-may/img-101.jpg';

describe('setAndroidGalleryWallpaper', () => {
  beforeEach(() => {
    cachePhoto.mockResolvedValue('file:///cache/img-101.jpg');
    setFromFile.mockResolvedValue(undefined);
  });

  it('caches the CDN image then sets each Android target', async () => {
    await setAndroidGalleryWallpaper(fullImage, 'home');
    await setAndroidGalleryWallpaper(fullImage, 'lock');
    await setAndroidGalleryWallpaper(fullImage, 'both');

    expect(cachePhoto).toHaveBeenCalledTimes(3);
    expect(cachePhoto).toHaveBeenCalledWith(fullImage, undefined);
    expect(setFromFile).toHaveBeenNthCalledWith(1, 'file:///cache/img-101.jpg', 'home');
    expect(setFromFile).toHaveBeenNthCalledWith(2, 'file:///cache/img-101.jpg', 'lock');
    expect(setFromFile).toHaveBeenNthCalledWith(3, 'file:///cache/img-101.jpg', 'both');
  });

  it('surfaces a home-target failure instead of a silent no-op', async () => {
    setFromFile.mockRejectedValueOnce(new Error(wallpaperCopy.homeFailed));

    await expect(setAndroidGalleryWallpaper(fullImage, 'home')).rejects.toMatchObject({
      message: wallpaperCopy.homeFailed,
    });
  });
});
