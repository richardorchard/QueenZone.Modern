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
  photoThumbMeta,
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
});
