import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import {
  isPhotoCdnUrl,
  photoCategoryPageSize,
  photoCdnSource,
  photoCountLabel,
  photoCounterLabel,
  photoDetailMeta,
  photoRangeLabel,
  photoSizeFromPath,
  photoSwipeCapturePx,
  photoSwipeDirection,
  photoSwipeEdgeGuardPx,
  photoSwipeMaxOffAxisPx,
  photoSwipeIsTap,
  photoSwipeShouldCapture,
  photoSwipeShouldStart,
  photoSwipeTapSlopPx,
  photoSwipeThresholdPx,
  photoThumbMeta,
  photoViewerParams,
  resolvedPhotoSize,
  schedulePhotoGallerySwipe,
} from './photoGalleryMeta.ts';

describe('photo gallery meta', () => {
  it('matches the website category page size', () => {
    assert.equal(photoCategoryPageSize, 24);
  });

  it('accepts only cdn.queenzone.org image URLs', () => {
    assert.equal(isPhotoCdnUrl('https://cdn.queenzone.org/brian-may/img-101-t.jpg'), true);
    assert.equal(isPhotoCdnUrl('https://cdn.queenzone.org/brian-may/img-101.jpg'), true);
    assert.equal(isPhotoCdnUrl(' https://cdn.queenzone.org/queen/img-201.jpg '), true);
    assert.equal(isPhotoCdnUrl('https://www.queenzone.org/photography/queen/201'), false);
    assert.equal(isPhotoCdnUrl('https://queenzone.azurewebsites.net/Brian_May/img-101.jpg'), false);
    assert.equal(isPhotoCdnUrl('/Brian_May/img-101.jpg'), false);
    assert.equal(isPhotoCdnUrl(null), false);
    assert.equal(isPhotoCdnUrl('   '), false);
  });

  it('builds a remote source only for CDN URLs', () => {
    assert.deepEqual(photoCdnSource('https://cdn.queenzone.org/queen/img-201.jpg'), {
      uri: 'https://cdn.queenzone.org/queen/img-201.jpg',
    });
    assert.equal(photoCdnSource('https://www.queenzone.org/files/img-201.jpg'), null);
  });

  it('formats category counts and page ranges', () => {
    assert.equal(photoCountLabel(1), '1 image');
    assert.equal(photoCountLabel(1240), '1,240 images');
    assert.equal(photoRangeLabel(1, 24, 50, 24), 'Showing 1–24 of 50');
    assert.equal(photoRangeLabel(2, 24, 50, 24), 'Showing 25–48 of 50');
    assert.equal(photoRangeLabel(1, 24, 0, 0), 'No images');
  });

  it('joins thumbnail and detail captions like the website', () => {
    assert.equal(photoThumbMeta({ year: 1986, pictureDimensionsLabel: '1920 x 1080' }), '1986 · 1920 x 1080');
    assert.equal(photoThumbMeta({ year: 1980, pictureDimensionsLabel: null }), '1980');
    assert.deepEqual(
      photoDetailMeta({
        year: 1986,
        categoryName: 'Brian May',
        pictureDimensionsLabel: '1920 x 1080',
        submittedByDisplayName: 'QueenFan86',
      }),
      ['1986', 'Brian May', '1920 x 1080', 'Submitted by QueenFan86'],
    );
    assert.equal(photoCounterLabel(0, 3), '1 / 3');
  });

  it('keeps a size filter on viewer prev/next params', () => {
    assert.deepEqual(photoViewerParams('brian-may', 101, 'desktop'), {
      slug: 'brian-may',
      picId: 101,
      size: 'desktop',
    });
    assert.deepEqual(photoViewerParams('queen', 201, ''), { slug: 'queen', picId: 201 });
    assert.deepEqual(photoViewerParams('queen', 201, '   '), { slug: 'queen', picId: 201 });
    assert.deepEqual(photoViewerParams('queen', 201), { slug: 'queen', picId: 201 });
  });

  it('clears a requested size when the API path dropped the filter', () => {
    assert.equal(photoSizeFromPath('/photography/brian-may/103?size=desktop'), 'desktop');
    assert.equal(photoSizeFromPath('/photography/brian-may/103'), undefined);
    assert.equal(photoSizeFromPath(null), undefined);
    assert.equal(
      resolvedPhotoSize('desktop', '/photography/brian-may/101?size=desktop'),
      'desktop',
    );
    assert.equal(resolvedPhotoSize('desktop', '/photography/brian-may/103'), undefined);
    assert.equal(resolvedPhotoSize('', '/photography/brian-may/101?size=desktop'), undefined);
  });

  it('maps a horizontal swipe onto previous and next', () => {
    assert.equal(photoSwipeDirection(photoSwipeThresholdPx, 0), 'previous');
    assert.equal(photoSwipeDirection(-photoSwipeThresholdPx, 8), 'next');
    assert.equal(photoSwipeDirection(photoSwipeThresholdPx - 1, 0), null);
    assert.equal(photoSwipeDirection(80, photoSwipeMaxOffAxisPx + 1), null);
    assert.equal(photoSwipeDirection(40, 50), null);
    assert.equal(photoSwipeDirection(Number.NaN, 0), null);
  });

  it('captures horizontal pans except from the iOS back edge', () => {
    assert.equal(photoSwipeShouldCapture(photoSwipeCapturePx + 1, 0, photoSwipeEdgeGuardPx), true);
    assert.equal(photoSwipeShouldCapture(40, 10, 80), true);
    assert.equal(photoSwipeShouldCapture(40, 10, photoSwipeEdgeGuardPx - 1), false);
    assert.equal(photoSwipeShouldCapture(photoSwipeCapturePx, 0, 80), false);
    assert.equal(photoSwipeShouldCapture(20, 30, 80), false);
    assert.equal(photoSwipeShouldCapture(Number.NaN, 0, 80), false);
  });

  it('starts a viewer gesture except from the iOS back edge', () => {
    assert.equal(photoSwipeShouldStart(photoSwipeEdgeGuardPx), true);
    assert.equal(photoSwipeShouldStart(photoSwipeEdgeGuardPx - 1), false);
    assert.equal(photoSwipeShouldStart(Number.NaN), false);
  });

  it('treats a short press as a chrome toggle tap', () => {
    assert.equal(photoSwipeIsTap(0, 0), true);
    assert.equal(photoSwipeIsTap(photoSwipeTapSlopPx, photoSwipeTapSlopPx), true);
    assert.equal(photoSwipeIsTap(photoSwipeTapSlopPx + 1, 0), false);
    assert.equal(photoSwipeIsTap(Number.NaN, 0), false);
  });

  it('defers gallery navigation until after the current turn', async () => {
    let called = false;
    schedulePhotoGallerySwipe(() => {
      called = true;
    });
    assert.equal(called, false);
    await new Promise((resolve) => setTimeout(resolve, 0));
    assert.equal(called, true);
  });
});
