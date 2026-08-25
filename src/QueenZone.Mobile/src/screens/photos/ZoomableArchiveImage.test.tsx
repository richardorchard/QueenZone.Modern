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
    onTouchesDown?: (event: Record<string, unknown>) => void;
    onTouchesMove?: (event: Record<string, unknown>, state: { activate: () => void }) => void;
  };
};

function recordedGestures(): { pinch: RecordedGesture; pan: RecordedGesture; tap: RecordedGesture } {
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

function panTouches(dx: number, dy: number, startPageX = 80) {
  const pan = recordedGestures().pan;
  const activate = jest.fn();
  act(() => {
    pan.handlers.onTouchesDown?.({ allTouches: [{ absoluteX: startPageX, x: 40, y: 40 }] });
    pan.handlers.onTouchesMove?.(
      { allTouches: [{ absoluteX: startPageX + dx, x: 40 + dx, y: 40 + dy }] },
      { activate },
    );
    pan.handlers.onUpdate?.({ translationX: dx, translationY: dy });
    pan.handlers.onEnd?.({
      translationX: dx,
      translationY: dy,
      absoluteX: startPageX + dx,
    });
  });
  return activate;
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

  it('activates pan while zoomed and ignores gallery swipe until reset', () => {
    const { onGallerySwipe, onToggleChrome } = renderZoom();
    pinchTo(2);
    const activate = panTouches(80, 10);
    expect(activate).toHaveBeenCalled();
    expect(onGallerySwipe).not.toHaveBeenCalled();
    expect(onToggleChrome).not.toHaveBeenCalled();
  });

  it('swipes previous and next at 1× and treats a short press as a chrome tap', () => {
    const { onGallerySwipe, onToggleChrome } = renderZoom();
    panTouches(80, 0);
    expect(onGallerySwipe).toHaveBeenCalledWith('previous');
    panTouches(-80, 0);
    expect(onGallerySwipe).toHaveBeenCalledWith('next');
    panTouches(2, 2);
    expect(onToggleChrome).toHaveBeenCalled();
  });

  it('does not swipe from the iOS back edge or when neighbors are disabled', () => {
    const { onGallerySwipe, onToggleChrome } = renderZoom({
      canSwipePrevious: false,
      canSwipeNext: false,
    });
    panTouches(80, 0, 10);
    panTouches(-80, 0);
    expect(onGallerySwipe).not.toHaveBeenCalled();
    expect(onToggleChrome).not.toHaveBeenCalled();
  });

  it('ignores empty touch lists', () => {
    renderZoom();
    const pan = recordedGestures().pan;
    const activate = jest.fn();
    act(() => {
      pan.handlers.onTouchesDown?.({ allTouches: [] });
      pan.handlers.onTouchesMove?.({ allTouches: [] }, { activate });
    });
    expect(activate).not.toHaveBeenCalled();
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
    panTouches(-80, 0);
    expect(onGallerySwipe).toHaveBeenCalledWith('next');
  });
});
