import { act, fireEvent, screen } from '@testing-library/react-native';
import type { ComponentProps } from 'react';
import { AccessibilityInfo } from 'react-native';
import { renderWithProviders } from '../../test/render';
import { ZoomableArchiveImage } from './ZoomableArchiveImage';

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
  zoomPan: RecordedGesture;
  tap: RecordedGesture;
  singleTap: RecordedGesture;
} {
  return jest.requireMock('react-native-gesture-handler').getRecordedGestures();
}

function renderZoom(overrides: Partial<ComponentProps<typeof ZoomableArchiveImage>> = {}) {
  const onGallerySwipe = jest.fn();
  const onToggleChrome = jest.fn();
  const result = renderWithProviders(
    <ZoomableArchiveImage
      source={{ uri: 'https://cdn.queenzone.org/brian-may/img-101.jpg' }}
      label="Live Aid"
      recyclingKey="photo-full-101"
      imageWidth={1600}
      imageHeight={900}
      resetKey={101}
      canSwipePrevious
      canSwipeNext
      onGallerySwipe={onGallerySwipe}
      onToggleChrome={onToggleChrome}
      {...overrides}
    />,
    { navigation: false },
  );
  fireEvent(screen.getByHintText(/Pinch or double tap to zoom/), 'layout', {
    nativeEvent: { layout: { x: 0, y: 0, width: 400, height: 800 } },
  });
  return { ...result, onGallerySwipe, onToggleChrome };
}

function pinchTo(scale: number, focalX = 200, focalY = 400) {
  const pinch = recordedGestures().pinch;
  act(() => {
    pinch.handlers.onBegin?.();
    pinch.handlers.onUpdate?.({ scale, focalX, focalY });
    pinch.handlers.onEnd?.();
  });
}

function galleryPanEnd(dx: number, dy: number, startPageX = 80) {
  const pan = recordedGestures().pan;
  const fail = jest.fn();
  act(() => {
    pan.handlers.onTouchesDown?.(
      { allTouches: [{ absoluteX: startPageX, x: 40, y: 40 }] },
      { fail },
    );
    pan.handlers.onEnd?.({
      translationX: dx,
      translationY: dy,
      absoluteX: startPageX + dx,
    });
  });
  return fail;
}

describe('ZoomableArchiveImage', () => {
  beforeEach(() => {
    jest.spyOn(AccessibilityInfo, 'announceForAccessibility').mockImplementation(() => {});
  });

  it('renders the labelled archive image', () => {
    renderZoom();
    expect(screen.getByLabelText('Live Aid')).toBeOnTheScreen();
  });

  it('announces pinch zoom and a reset back to fit', () => {
    renderZoom();
    pinchTo(2);
    expect(AccessibilityInfo.announceForAccessibility).toHaveBeenCalledWith('Zoomed to 2 times');
    pinchTo(0.4);
    expect(AccessibilityInfo.announceForAccessibility).toHaveBeenCalledWith('Fit to screen');
  });

  it('pans while zoomed and ignores gallery swipe until reset', () => {
    const { onGallerySwipe, onToggleChrome } = renderZoom();
    pinchTo(2);
    act(() => {
      recordedGestures().zoomPan.handlers.onUpdate?.({ translationX: 80, translationY: 10 });
      recordedGestures().zoomPan.handlers.onEnd?.({ translationX: 80, translationY: 10 });
    });
    expect(onGallerySwipe).not.toHaveBeenCalled();
    expect(onToggleChrome).not.toHaveBeenCalled();
  });

  it('swipes previous and next at 1× and treats a tap as a chrome toggle', () => {
    const { onGallerySwipe, onToggleChrome } = renderZoom();
    galleryPanEnd(80, 0);
    expect(onGallerySwipe).toHaveBeenCalledWith('previous');
    galleryPanEnd(-80, 0);
    expect(onGallerySwipe).toHaveBeenCalledWith('next');
    act(() => recordedGestures().singleTap.handlers.onEnd?.({}));
    expect(onToggleChrome).toHaveBeenCalled();
  });

  it('does not swipe from the iOS back edge or when neighbors are disabled', () => {
    const { onGallerySwipe, onToggleChrome } = renderZoom({
      canSwipePrevious: false,
      canSwipeNext: false,
    });
    const fail = galleryPanEnd(80, 0, 10);
    expect(fail).toHaveBeenCalled();
    galleryPanEnd(-80, 0);
    expect(onGallerySwipe).not.toHaveBeenCalled();
    expect(onToggleChrome).not.toHaveBeenCalled();
  });

  it('ignores empty touch lists', () => {
    renderZoom();
    const pan = recordedGestures().pan;
    const fail = jest.fn();
    act(() => {
      pan.handlers.onTouchesDown?.({ allTouches: [] }, { fail });
      pan.handlers.onTouchesDown?.({}, { fail });
    });
    expect(fail).not.toHaveBeenCalled();
  });

  it('double-taps to zoom in and back to fit', () => {
    renderZoom();
    const tap = recordedGestures().tap;
    act(() => tap.handlers.onEnd?.({ x: 300, y: 200 }));
    expect(AccessibilityInfo.announceForAccessibility).toHaveBeenCalledWith('Zoomed to 2 times');
    act(() => tap.handlers.onEnd?.({ x: 300, y: 200 }));
    expect(AccessibilityInfo.announceForAccessibility).toHaveBeenCalledWith('Fit to screen');
  });

  it('resets zoom when the photograph changes', () => {
    const { rerender, onGallerySwipe } = renderZoom();
    pinchTo(2);
    rerender(
      <ZoomableArchiveImage
        source={{ uri: 'https://cdn.queenzone.org/brian-may/img-102.jpg' }}
        label="Wembley"
        recyclingKey="photo-full-102"
        imageWidth={900}
        imageHeight={1600}
        resetKey={102}
        canSwipePrevious
        canSwipeNext
        onGallerySwipe={onGallerySwipe}
        onToggleChrome={jest.fn()}
      />,
    );
    fireEvent(screen.getByHintText(/Pinch or double tap to zoom/), 'layout', {
      nativeEvent: { layout: { x: 0, y: 0, width: 400, height: 800 } },
    });
    galleryPanEnd(-80, 0);
    expect(onGallerySwipe).toHaveBeenCalledWith('next');
  });
});
