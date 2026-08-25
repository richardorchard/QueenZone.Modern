import { act, fireEvent, screen, userEvent, waitFor } from '@testing-library/react-native';
import { fetchPhotoDetail } from '../../api';
import { ApiError } from '../../api/client';
import type { PhotoDetail } from '../../api/types';
import { deferred } from '../../test/fixtures';
import { fakeNavigation, renderWithProviders } from '../../test/render';
import { testIds } from '../../test/testIds';
import { PhotoViewerScreen } from './PhotoViewerScreen';

jest.mock('../../api', () => {
  const actual = jest.requireActual('../../api');
  return {
    ...actual,
    fetchPhotoDetail: jest.fn(),
  };
});

const fetchPhoto = fetchPhotoDetail as jest.MockedFunction<typeof fetchPhotoDetail>;

type RecordedGesture = {
  handlers: {
    onBegin?: () => void;
    onUpdate?: (event: Record<string, unknown>) => void;
    onEnd?: (event: Record<string, unknown>) => void;
    onTouchesDown?: (event: Record<string, unknown>) => void;
    onTouchesMove?: (event: Record<string, unknown>, state: { activate: () => void }) => void;
  };
};

function recordedGestures(): { pinch: RecordedGesture; pan: RecordedGesture; tap: RecordedGesture } {
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

describe('PhotoViewerScreen', () => {
  beforeEach(() => {
    fetchPhoto.mockReset();
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
    expect(navigation.setParams).toHaveBeenCalledWith({ slug: 'brian-may', picId: 100 });
    panEnd(-80, 0);
    expect(navigation.setParams).toHaveBeenCalledWith({ slug: 'brian-may', picId: 102 });
    panEnd(0, 0);
    expect(screen.queryByText('Live Aid')).toBeNull();
    panEnd(0, 0);
    expect(screen.getByText('Live Aid')).toBeOnTheScreen();
  });

  it('does not swipe from the iOS back edge or when a neighbor is missing', async () => {
    const { navigation } = await loadPhoto(photoDetail({ previous: null, next: null }));
    panEnd(80, 0, 10);
    panEnd(-80, 0);
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
});
