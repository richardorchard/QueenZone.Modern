import { act, screen, userEvent, waitFor } from '@testing-library/react-native';
import { Platform } from 'react-native';
import { fetchPhotoDetail } from '../../api';
import { ApiError } from '../../api/client';
import type { PhotoDetail } from '../../api/types';
import { SaveToPhotosError, saveToPhotosCopy } from '../../media/saveToPhotos';
import { deferred } from '../../test/fixtures';
import { fakeNavigation, renderWithProviders } from '../../test/render';
import { testIds } from '../../test/testIds';
import { PhotoViewerScreen } from './PhotoViewerScreen';
import { saveGalleryPhoto } from './saveGalleryPhoto';
import { setAndroidGalleryWallpaper } from './setGalleryWallpaper';
import { wallpaperCopy } from './wallpaperMeta';

jest.mock('../../api', () => {
  const actual = jest.requireActual('../../api');
  return {
    ...actual,
    fetchPhotoDetail: jest.fn(),
  };
});

jest.mock('./saveGalleryPhoto', () => {
  const actual = jest.requireActual('./saveGalleryPhoto');
  return {
    ...actual,
    saveGalleryPhoto: jest.fn(),
  };
});

jest.mock('./setGalleryWallpaper', () => ({
  setAndroidGalleryWallpaper: jest.fn(),
}));

const fetchPhoto = fetchPhotoDetail as jest.MockedFunction<typeof fetchPhotoDetail>;
const savePhoto = saveGalleryPhoto as jest.MockedFunction<typeof saveGalleryPhoto>;
const setWallpaper = setAndroidGalleryWallpaper as jest.MockedFunction<
  typeof setAndroidGalleryWallpaper
>;

type RecordedGesture = {
  handlers: {
    onBegin?: () => void;
    onUpdate?: (event: Record<string, unknown>) => void;
    onEnd?: (event: Record<string, unknown>) => void;
    onTouchesDown?: (event: Record<string, unknown>, state: { fail: () => void }) => void;
  };
};

function recordedGestures(): {
  pinch: RecordedGesture;
  pan: RecordedGesture;
  singleTap: RecordedGesture;
} {
  return jest.requireMock('react-native-gesture-handler').getRecordedGestures();
}

function photoDetail(overrides: Partial<PhotoDetail> = {}): PhotoDetail {
  return {
    picId: 101,
    catId: 1,
    categoryName: 'Brian May',
    categorySlug: 'brian-may',
    title: 'Live Aid',
    imageUrl: 'https://cdn.queenzone.org/brian-may/img-101.jpg',
    thumbnailUrl: 'https://cdn.queenzone.org/brian-may/img-101-t.jpg',
    thumbWidth: 200,
    thumbHeight: 150,
    pictureWidth: 1600,
    pictureHeight: 900,
    pictureDimensionsLabel: '1600 x 900',
    year: 1985,
    dateTime: '1985-07-13T00:00:00.000Z',
    submittedByDisplayName: 'QueenFan',
    detailPath: '/photography/brian-may/101',
    categoryPath: '/photography/brian-may',
    index: 0,
    count: 3,
    previous: { picId: 100, detailPath: '/photography/brian-may/100' },
    next: { picId: 102, detailPath: '/photography/brian-may/102' },
    ...overrides,
  };
}

function renderViewer(
  navigation = fakeNavigation(),
  params: { slug?: string; picId?: number; size?: string } = {},
) {
  return {
    navigation,
    ...renderWithProviders(
      <PhotoViewerScreen
        navigation={navigation as never}
        route={
          {
            key: 'viewer',
            name: 'PhotoViewer',
            params: { slug: 'brian-may', picId: 101, ...params },
          } as never
        }
      />,
      { navigation: false },
    ),
  };
}

async function loadPhoto(detail: PhotoDetail = photoDetail()) {
  fetchPhoto.mockResolvedValueOnce(detail);
  const result = renderViewer();
  await waitFor(() => expect(screen.getByTestId(testIds.photoViewerScreen)).toBeOnTheScreen());
  return result;
}

function panEnd(dx: number, dy: number, startPageX = 80) {
  act(() => {
    recordedGestures().pan.handlers.onEnd?.({
      translationX: dx,
      translationY: dy,
      absoluteX: startPageX + dx,
    });
  });
}

async function flushGallerySwipe() {
  await act(async () => {
    await new Promise((resolve) => setTimeout(resolve, 0));
  });
}

describe('PhotoViewerScreen', () => {
  beforeEach(() => {
    fetchPhoto.mockReset();
    savePhoto.mockReset();
    savePhoto.mockResolvedValue(undefined);
    setWallpaper.mockReset();
    setWallpaper.mockResolvedValue(undefined);
  });

  it('shows loading then the photograph chrome', async () => {
    const pending = deferred<PhotoDetail>();
    fetchPhoto.mockReturnValueOnce(pending.promise);
    renderViewer();
    expect(screen.getByLabelText('Loading photograph…')).toBeOnTheScreen();
    pending.resolve(photoDetail());
    await waitFor(() => expect(screen.getByText('Live Aid')).toBeOnTheScreen());
    expect(screen.getByText('1 / 3')).toBeOnTheScreen();
  });

  it('ignores abort when the screen unmounts during load', async () => {
    fetchPhoto.mockImplementation(
      (_slug, _picId, query = {}) =>
        new Promise((_resolve, reject) => {
          const abort = () => {
            const error = new Error('Aborted');
            error.name = 'AbortError';
            reject(error);
          };
          if (query.signal?.aborted) {
            abort();
            return;
          }
          query.signal?.addEventListener('abort', abort);
        }),
    );
    const { unmount } = renderViewer();
    unmount();
    await Promise.resolve();
    await Promise.resolve();
  });

  it('shows an API error and retries', async () => {
    fetchPhoto
      .mockRejectedValueOnce(new ApiError(500, 'The server had a problem. Try again shortly.'))
      .mockResolvedValueOnce(photoDetail());
    renderViewer();
    await waitFor(() => expect(screen.getByText('Unable to load')).toBeOnTheScreen());
    expect(screen.getByText('The server had a problem. Try again shortly.')).toBeOnTheScreen();
    const user = userEvent.setup();
    await user.press(screen.getByRole('button', { name: 'Try again' }));
    await waitFor(() => expect(screen.getByText('Live Aid')).toBeOnTheScreen());
  });

  it('shows a generic error for unexpected failures', async () => {
    fetchPhoto.mockRejectedValueOnce(new Error('boom'));
    renderViewer();
    await waitFor(() => expect(screen.getByText('Something went wrong.')).toBeOnTheScreen());
  });

  it('clears a size filter the API dropped', async () => {
    const navigation = fakeNavigation();
    fetchPhoto.mockResolvedValueOnce(photoDetail({ detailPath: '/photography/brian-may/101' }));
    renderWithProviders(
      <PhotoViewerScreen
        navigation={navigation as never}
        route={
          {
            key: 'viewer',
            name: 'PhotoViewer',
            params: { slug: 'brian-may', picId: 101, size: 'desktop' },
          } as never
        }
      />,
      { navigation: false },
    );
    await waitFor(() => expect(navigation.setParams).toHaveBeenCalledWith({ size: '' }));
  });

  it('keeps a size filter that the detail path still carries', async () => {
    const navigation = fakeNavigation();
    fetchPhoto.mockResolvedValueOnce(
      photoDetail({ detailPath: '/photography/brian-may/101?size=desktop' }),
    );
    renderWithProviders(
      <PhotoViewerScreen
        navigation={navigation as never}
        route={
          {
            key: 'viewer',
            name: 'PhotoViewer',
            params: { slug: 'brian-may', picId: 101, size: 'desktop' },
          } as never
        }
      />,
      { navigation: false },
    );
    await waitFor(() => expect(screen.getByText('Live Aid')).toBeOnTheScreen());
    expect(navigation.setParams).not.toHaveBeenCalled();
  });

  it('keeps size on previous and next when the detail path still carries it', async () => {
    const navigation = fakeNavigation();
    fetchPhoto.mockResolvedValueOnce(
      photoDetail({
        detailPath: '/photography/brian-may/101?size=desktop',
        previous: { picId: 100, detailPath: '/photography/brian-may/100?size=desktop' },
        next: { picId: 102, detailPath: '/photography/brian-may/102?size=desktop' },
      }),
    );
    renderWithProviders(
      <PhotoViewerScreen
        navigation={navigation as never}
        route={
          {
            key: 'viewer',
            name: 'PhotoViewer',
            params: { slug: 'brian-may', picId: 101, size: 'desktop' },
          } as never
        }
      />,
      { navigation: false },
    );
    await waitFor(() => expect(screen.getByText('Live Aid')).toBeOnTheScreen());
    expect(fetchPhoto).toHaveBeenCalledWith(
      'brian-may',
      101,
      expect.objectContaining({ size: 'desktop' }),
    );
    expect(navigation.setParams).not.toHaveBeenCalled();

    const user = userEvent.setup();
    await user.press(screen.getByRole('button', { name: 'Previous image' }));
    expect(navigation.setParams).toHaveBeenCalledWith({
      slug: 'brian-may',
      picId: 100,
      size: 'desktop',
    });
    await user.press(screen.getByRole('button', { name: 'Next image' }));
    expect(navigation.setParams).toHaveBeenCalledWith({
      slug: 'brian-may',
      picId: 102,
      size: 'desktop',
    });
  });

  it('closes and steps through previous and next from the chrome buttons', async () => {
    const { navigation } = await loadPhoto();
    const user = userEvent.setup();
    await user.press(screen.getByRole('button', { name: 'Close' }));
    expect(navigation.goBack).toHaveBeenCalled();
    await user.press(screen.getByRole('button', { name: 'Previous image' }));
    expect(navigation.setParams).toHaveBeenCalledWith({ slug: 'brian-may', picId: 100 });
    await user.press(screen.getByRole('button', { name: 'Next image' }));
    expect(navigation.setParams).toHaveBeenCalledWith({ slug: 'brian-may', picId: 102 });
  });

  it('omits neighbor buttons and shows a fallback when the image URL is not on the CDN', async () => {
    await loadPhoto(
      photoDetail({
        imageUrl: 'https://www.queenzone.org/not-cdn.jpg',
        previous: null,
        next: null,
      }),
    );
    expect(screen.getByText('Image unavailable')).toBeOnTheScreen();
    expect(screen.queryByRole('button', { name: 'Previous image' })).toBeNull();
    expect(screen.queryByRole('button', { name: 'Next image' })).toBeNull();
  });

  it('swipes to neighbors and toggles chrome from a tap', async () => {
    const { navigation } = await loadPhoto();
    panEnd(80, 0);
    expect(navigation.setParams).not.toHaveBeenCalled();
    await flushGallerySwipe();
    expect(navigation.setParams).toHaveBeenCalledWith({ slug: 'brian-may', picId: 100 });
    panEnd(-80, 0);
    await flushGallerySwipe();
    expect(navigation.setParams).toHaveBeenCalledWith({ slug: 'brian-may', picId: 102 });
    act(() => recordedGestures().singleTap.handlers.onEnd?.({}));
    expect(screen.queryByText('Live Aid')).toBeNull();
    act(() => recordedGestures().singleTap.handlers.onEnd?.({}));
    expect(screen.getByText('Live Aid')).toBeOnTheScreen();
  });

  it('does not swipe from the iOS back edge or when a neighbor is missing', async () => {
    const { navigation } = await loadPhoto(photoDetail({ previous: null, next: null }));
    panEnd(80, 0, 10);
    panEnd(-80, 0);
    await flushGallerySwipe();
    expect(navigation.setParams).not.toHaveBeenCalled();
  });

  it('does not navigate after the viewer unmounts mid-swipe', async () => {
    const { navigation, unmount } = await loadPhoto();
    panEnd(-80, 0);
    unmount();
    await flushGallerySwipe();
    expect(navigation.setParams).not.toHaveBeenCalled();
  });

  it('ignores chrome navigation after the route has already moved on', async () => {
    const pendingNext = deferred<PhotoDetail>();
    fetchPhoto.mockResolvedValueOnce(photoDetail()).mockReturnValueOnce(pendingNext.promise);
    const navigation = fakeNavigation();
    const view = renderWithProviders(
      <PhotoViewerScreen
        navigation={navigation as never}
        route={
          {
            key: 'viewer',
            name: 'PhotoViewer',
            params: { slug: 'brian-may', picId: 101 },
          } as never
        }
      />,
      { navigation: false },
    );
    await waitFor(() => expect(screen.getByRole('button', { name: 'Previous image' })).toBeOnTheScreen());
    view.rerender(
      <PhotoViewerScreen
        navigation={navigation as never}
        route={
          {
            key: 'viewer',
            name: 'PhotoViewer',
            params: { slug: 'brian-may', picId: 102 },
          } as never
        }
      />,
    );
    const user = userEvent.setup();
    await user.press(screen.getByRole('button', { name: 'Previous image' }));
    expect(navigation.setParams).not.toHaveBeenCalled();
    pendingNext.resolve(photoDetail({ picId: 102, title: 'Wembley', index: 1 }));
    await waitFor(() => expect(screen.getByText('Wembley')).toBeOnTheScreen());
  });

  it('saves the full imageUrl from the viewer chrome, not the thumbnail', async () => {
    await loadPhoto();
    expect(screen.getByTestId(testIds.photoViewerSave)).toBeOnTheScreen();
    expect(screen.getByTestId(testIds.photoViewerWallpaper)).toBeOnTheScreen();
    expect(screen.getByRole('button', { name: 'Save to Photos' })).toBeOnTheScreen();
    expect(screen.getByRole('button', { name: 'Set as wallpaper' })).toBeOnTheScreen();
    expect(screen.getByRole('button', { name: 'Close' })).toBeOnTheScreen();
    expect(screen.getByText('Live Aid')).toBeOnTheScreen();

    const user = userEvent.setup();
    await user.press(screen.getByTestId(testIds.photoViewerSave));
    await waitFor(() =>
      expect(savePhoto).toHaveBeenCalledWith('https://cdn.queenzone.org/brian-may/img-101.jpg'),
    );
    expect(savePhoto).not.toHaveBeenCalledWith(
      'https://cdn.queenzone.org/brian-may/img-101-t.jpg',
    );
  });

  it('hides Save with the rest of the chrome and shows a permission-denied error', async () => {
    savePhoto.mockRejectedValueOnce(
      new SaveToPhotosError('permission-denied', saveToPhotosCopy.denied),
    );
    await loadPhoto();
    const user = userEvent.setup();
    await user.press(screen.getByTestId(testIds.photoViewerSave));
    await waitFor(() => expect(screen.getByText(saveToPhotosCopy.denied)).toBeOnTheScreen());

    act(() => recordedGestures().singleTap.handlers.onEnd?.({}));
    expect(screen.queryByTestId(testIds.photoViewerSave)).toBeNull();
    expect(screen.queryByTestId(testIds.photoViewerWallpaper)).toBeNull();
    expect(screen.queryByText('Live Aid')).toBeNull();
    act(() => recordedGestures().singleTap.handlers.onEnd?.({}));
    expect(screen.getByTestId(testIds.photoViewerSave)).toBeOnTheScreen();
    expect(screen.getByTestId(testIds.photoViewerWallpaper)).toBeOnTheScreen();
    expect(screen.getByText('Live Aid')).toBeOnTheScreen();
  });

  it('ignores a second Save tap while the first is in flight', async () => {
    const pending = deferred<void>();
    savePhoto.mockReturnValueOnce(pending.promise);
    await loadPhoto();
    const user = userEvent.setup();
    await user.press(screen.getByTestId(testIds.photoViewerSave));
    await user.press(screen.getByTestId(testIds.photoViewerSave));
    expect(savePhoto).toHaveBeenCalledTimes(1);
    pending.resolve();
    await waitFor(() => expect(savePhoto).toHaveBeenCalledTimes(1));
  });

  it('shows a fallback save error for unexpected failures', async () => {
    savePhoto.mockRejectedValueOnce('boom');
    await loadPhoto();
    const user = userEvent.setup();
    await user.press(screen.getByTestId(testIds.photoViewerSave));
    await waitFor(() =>
      expect(screen.getByText('Unable to save this picture.')).toBeOnTheScreen(),
    );
  });

  it('omits Save when the image URL is not on the CDN', async () => {
    await loadPhoto(
      photoDetail({
        imageUrl: 'https://www.queenzone.org/not-cdn.jpg',
        previous: null,
        next: null,
      }),
    );
    expect(screen.queryByTestId(testIds.photoViewerSave)).toBeNull();
    expect(screen.queryByTestId(testIds.photoViewerWallpaper)).toBeNull();
    expect(screen.queryByRole('button', { name: 'Save to Photos' })).toBeNull();
    expect(screen.queryByRole('button', { name: 'Set as wallpaper' })).toBeNull();
  });

  it('saves to Photos on iOS and never claims wallpaper was set', async () => {
    await loadPhoto();
    const user = userEvent.setup();
    await user.press(screen.getByTestId(testIds.photoViewerWallpaper));
    await waitFor(() =>
      expect(savePhoto).toHaveBeenCalledWith('https://cdn.queenzone.org/brian-may/img-101.jpg'),
    );
    expect(screen.getByText(wallpaperCopy.iosSaved)).toBeOnTheScreen();
    expect(screen.queryByText(wallpaperCopy.androidSet)).toBeNull();
    expect(screen.queryByText(wallpaperCopy.home)).toBeNull();
    expect(screen.queryByTestId(testIds.photoViewerWallpaperSheet)).toBeNull();
    expect(setWallpaper).not.toHaveBeenCalled();
  });

  it('shows the save error when the iOS wallpaper save fails', async () => {
    savePhoto.mockRejectedValueOnce(
      new SaveToPhotosError('permission-denied', saveToPhotosCopy.denied),
    );
    await loadPhoto();
    const user = userEvent.setup();
    await user.press(screen.getByTestId(testIds.photoViewerWallpaper));
    await waitFor(() => expect(screen.getByText(saveToPhotosCopy.denied)).toBeOnTheScreen());
    expect(screen.queryByText(wallpaperCopy.androidSet)).toBeNull();
    expect(screen.queryByText(wallpaperCopy.iosSaved)).toBeNull();
  });

  it('ignores a second iOS wallpaper tap while the save is in flight', async () => {
    const pending = deferred<void>();
    savePhoto.mockReturnValueOnce(pending.promise);
    await loadPhoto();
    const user = userEvent.setup();
    await user.press(screen.getByTestId(testIds.photoViewerWallpaper));
    await user.press(screen.getByTestId(testIds.photoViewerWallpaper));
    expect(savePhoto).toHaveBeenCalledTimes(1);
    pending.resolve();
    await waitFor(() => expect(savePhoto).toHaveBeenCalledTimes(1));
  });
});

describe('PhotoViewerScreen Android wallpaper', () => {
  const originalOs = Platform.OS;

  beforeEach(() => {
    Object.defineProperty(Platform, 'OS', { configurable: true, value: 'android' });
    fetchPhoto.mockReset();
    savePhoto.mockReset();
    savePhoto.mockResolvedValue(undefined);
    setWallpaper.mockReset();
    setWallpaper.mockResolvedValue(undefined);
  });

  afterEach(() => {
    Object.defineProperty(Platform, 'OS', { configurable: true, value: originalOs });
  });

  it('opens a Home / Lock / Both sheet and sets the chosen target', async () => {
    await loadPhoto();
    const user = userEvent.setup();
    await user.press(screen.getByTestId(testIds.photoViewerWallpaper));
    expect(screen.getByTestId(testIds.photoViewerWallpaperSheet)).toBeOnTheScreen();
    expect(screen.getByRole('button', { name: wallpaperCopy.home })).toBeOnTheScreen();
    expect(screen.getByRole('button', { name: wallpaperCopy.lock })).toBeOnTheScreen();
    expect(screen.getByRole('button', { name: wallpaperCopy.both })).toBeOnTheScreen();
    expect(savePhoto).not.toHaveBeenCalled();

    await user.press(screen.getByTestId(testIds.photoViewerWallpaperHome));
    await waitFor(() =>
      expect(setWallpaper).toHaveBeenCalledWith(
        'https://cdn.queenzone.org/brian-may/img-101.jpg',
        'home',
      ),
    );
    expect(screen.getByText(wallpaperCopy.androidSet)).toBeOnTheScreen();
    expect(screen.queryByTestId(testIds.photoViewerWallpaperSheet)).toBeNull();
  });

  it('sets lock and both from the sheet', async () => {
    await loadPhoto();
    const user = userEvent.setup();
    await user.press(screen.getByTestId(testIds.photoViewerWallpaper));
    await user.press(screen.getByTestId(testIds.photoViewerWallpaperLock));
    await waitFor(() =>
      expect(setWallpaper).toHaveBeenCalledWith(
        'https://cdn.queenzone.org/brian-may/img-101.jpg',
        'lock',
      ),
    );

    await user.press(screen.getByTestId(testIds.photoViewerWallpaper));
    await user.press(screen.getByTestId(testIds.photoViewerWallpaperBoth));
    await waitFor(() =>
      expect(setWallpaper).toHaveBeenCalledWith(
        'https://cdn.queenzone.org/brian-may/img-101.jpg',
        'both',
      ),
    );
  });

  it('shows a lock-target error instead of a silent no-op', async () => {
    setWallpaper.mockRejectedValueOnce(new Error(wallpaperCopy.lockFailed));
    await loadPhoto();
    const user = userEvent.setup();
    await user.press(screen.getByTestId(testIds.photoViewerWallpaper));
    await user.press(screen.getByTestId(testIds.photoViewerWallpaperLock));
    await waitFor(() => expect(screen.getByText(wallpaperCopy.lockFailed)).toBeOnTheScreen());
    expect(screen.queryByText(wallpaperCopy.androidSet)).toBeNull();
  });

  it('cancels the sheet without setting wallpaper', async () => {
    await loadPhoto();
    const user = userEvent.setup();
    await user.press(screen.getByTestId(testIds.photoViewerWallpaper));
    await user.press(screen.getByTestId(testIds.photoViewerWallpaperCancel));
    expect(screen.queryByTestId(testIds.photoViewerWallpaperSheet)).toBeNull();
    expect(setWallpaper).not.toHaveBeenCalled();
    expect(savePhoto).not.toHaveBeenCalled();
  });
});
