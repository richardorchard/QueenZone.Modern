import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import {
  clampPhotoPanTranslation,
  clampPhotoZoomScale,
  containedImageSize,
  focalPhotoZoomTranslation,
  isPhotoZoomed,
  photoPanBounds,
  photoPanShouldActivate,
  photoZoomAccessibilityMessage,
  photoZoomDoubleTapScale,
  photoZoomMaxScale,
  photoZoomMinScale,
} from './photoZoomMeta.ts';
import { photoSwipeCapturePx, photoSwipeEdgeGuardPx } from './photoGalleryMeta.ts';

describe('photo zoom meta', () => {
  it('defines the supported zoom range', () => {
    assert.equal(photoZoomMinScale, 1);
    assert.equal(photoZoomMaxScale, 4);
    assert.equal(photoZoomDoubleTapScale, 2);
  });

  it('clamps pinch scale inside the supported range', () => {
    assert.equal(clampPhotoZoomScale(0.5), photoZoomMinScale);
    assert.equal(clampPhotoZoomScale(2.5), 2.5);
    assert.equal(clampPhotoZoomScale(photoZoomMaxScale + 1), photoZoomMaxScale);
    assert.equal(clampPhotoZoomScale(Number.NaN), photoZoomMinScale);
  });

  it('detects when the viewer is zoomed in', () => {
    assert.equal(isPhotoZoomed(photoZoomMinScale), false);
    assert.equal(isPhotoZoomed(photoZoomDoubleTapScale), true);
  });

  it('computes contain-fit dimensions for landscape and portrait images', () => {
    assert.deepEqual(containedImageSize({ width: 400, height: 800 }, { width: 1600, height: 900 }), {
      width: 400,
      height: 225,
    });
    assert.deepEqual(containedImageSize({ width: 300, height: 600 }, { width: 300, height: 600 }), {
      width: 300,
      height: 600,
    });
    assert.deepEqual(containedImageSize({ width: 0, height: 800 }, { width: 1600, height: 900 }), {
      width: 0,
      height: 0,
    });
  });

  it('allows no pan at 1× and clamps pan when zoomed', () => {
    const container = { width: 400, height: 800 };
    const image = { width: 1600, height: 900 };

    assert.deepEqual(photoPanBounds(1, container, image), { maxX: 0, maxY: 0 });
    assert.deepEqual(photoPanBounds(2, container, image), { maxX: 200, maxY: 0 });

    assert.deepEqual(clampPhotoPanTranslation(-500, -500, 2, container, image), { x: -200, y: 0 });
    const clampedPositive = clampPhotoPanTranslation(500, 500, 2, container, image);
    assert.equal(clampedPositive.x, 200);
    assert.equal(clampedPositive.y, 0);
  });

  it('keeps the focal point stable when scale changes', () => {
    const container = { width: 400, height: 400 };
    const next = focalPhotoZoomTranslation(0, 0, 1, 2, 300, 200, container);
    assert.equal(next.x, -100);
    assert.equal(next.y, 0);
    assert.deepEqual(focalPhotoZoomTranslation(5, 7, 0, 2, 300, 200, container), { x: 5, y: 7 });
  });

  it('matches gallery swipe activation rules at 1×', () => {
    assert.equal(
      photoPanShouldActivate(
        photoSwipeCapturePx + 1,
        0,
        photoSwipeEdgeGuardPx,
        false,
        photoSwipeCapturePx,
        photoSwipeEdgeGuardPx,
      ),
      true,
    );
    assert.equal(
      photoPanShouldActivate(
        photoSwipeCapturePx + 1,
        0,
        photoSwipeEdgeGuardPx - 1,
        false,
        photoSwipeCapturePx,
        photoSwipeEdgeGuardPx,
      ),
      false,
    );
    assert.equal(
      photoPanShouldActivate(0, 0, 80, true, photoSwipeCapturePx, photoSwipeEdgeGuardPx),
      true,
    );
  });

  it('formats accessibility copy for fit and zoomed states', () => {
    assert.equal(photoZoomAccessibilityMessage(1), 'Fit to screen');
    assert.equal(photoZoomAccessibilityMessage(2), 'Zoomed to 2 times');
    assert.equal(photoZoomAccessibilityMessage(2.25), 'Zoomed to 2.3 times');
  });
});
