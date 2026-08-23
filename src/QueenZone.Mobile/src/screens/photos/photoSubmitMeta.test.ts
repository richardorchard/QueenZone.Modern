import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import {
  archiveImagePickerOptions,
  defaultPhotoFileName,
  formatSubmittedAt,
  normalizePhotoMimeType,
  parseApproximateDate,
  parseApproximateYear,
  photoFromPickerAsset,
  photoMaxUploadBytes,
  photoSubmitCopy,
  photoTitleMaxLength,
  validatePhotoSubmit,
} from './photoSubmitMeta.ts';

describe('photoSubmitCopy', () => {
  it('matches the website confirmation message', () => {
    assert.equal(photoSubmitCopy.confirmationTitle, 'Photo submitted');
    assert.equal(photoSubmitCopy.confirmationMessage, 'Your photo is under review.');
    assert.match(photoSubmitCopy.help, /20 MB/);
  });
});

describe('archiveImagePickerOptions', () => {
  it('keeps the original image without cropping', () => {
    assert.deepEqual(archiveImagePickerOptions.mediaTypes, ['images']);
    assert.equal(archiveImagePickerOptions.quality, 1);
    assert.equal(archiveImagePickerOptions.allowsEditing, false);
  });
});

describe('normalizePhotoMimeType', () => {
  it('accepts website upload types and infers from the file name', () => {
    assert.equal(normalizePhotoMimeType('image/jpg', 'crowd.jpg'), 'image/jpeg');
    assert.equal(normalizePhotoMimeType('image/png', 'crowd.png'), 'image/png');
    assert.equal(normalizePhotoMimeType(null, 'crowd.tiff'), 'image/tiff');
    assert.equal(normalizePhotoMimeType('image/heic', 'crowd.heic'), null);
    assert.equal(defaultPhotoFileName('image/jpeg', 'file:///tmp/IMG_001.HEIC'), 'photo.jpg');
  });
});

describe('photoFromPickerAsset', () => {
  it('maps a library asset onto the multipart photo part', () => {
    const mapped = photoFromPickerAsset({
      uri: 'file:///photos/crowd.jpg',
      fileName: 'crowd.jpg',
      mimeType: 'image/jpeg',
      fileSize: 2048,
    });
    assert.ok('photo' in mapped);
    if ('photo' in mapped) {
      assert.equal(mapped.photo.name, 'crowd.jpg');
      assert.equal(mapped.photo.type, 'image/jpeg');
      assert.equal(mapped.fileSize, 2048);
    }
  });

  it('rejects HEIC leftovers and oversized files before upload', () => {
    const heic = photoFromPickerAsset({
      uri: 'file:///photos/crowd.heic',
      fileName: 'crowd.heic',
      mimeType: 'image/heic',
    });
    assert.deepEqual(heic, { error: 'Photo must be a JPEG, PNG, WebP, or TIFF image.' });

    const huge = photoFromPickerAsset({
      uri: 'file:///photos/crowd.jpg',
      fileName: 'crowd.jpg',
      mimeType: 'image/jpeg',
      fileSize: photoMaxUploadBytes + 1,
    });
    assert.deepEqual(huge, { error: 'Photo must be 20 MB or smaller.' });
  });
});

describe('validatePhotoSubmit', () => {
  const photo = { uri: 'file:///photos/crowd.jpg', name: 'crowd.jpg', type: 'image/jpeg' };

  it('requires the same title and photo as /submit/photo', () => {
    assert.equal(
      validatePhotoSubmit({
        title: '   ',
        description: '',
        suggestedCategory: '',
        approximateYear: '',
        approximateDate: '',
        photo,
      }),
      'Title is required.',
    );
    assert.equal(
      validatePhotoSubmit({
        title: 'Wembley',
        description: '',
        suggestedCategory: '',
        approximateYear: '',
        approximateDate: '',
        photo: null,
      }),
      'Choose a photo to upload.',
    );
    assert.equal(
      validatePhotoSubmit({
        title: 'W'.repeat(photoTitleMaxLength + 1),
        description: '',
        suggestedCategory: '',
        approximateYear: '',
        approximateDate: '',
        photo,
      }),
      'Title must be 200 characters or fewer.',
    );
    assert.equal(
      validatePhotoSubmit({
        title: 'Wembley crowd shot',
        description: 'From the stands',
        suggestedCategory: 'Queen',
        approximateYear: '1986',
        approximateDate: '1986-07-12',
        photo,
        fileSize: 1024,
      }),
      null,
    );
  });

  it('validates optional year and date the same way as the web form', () => {
    assert.deepEqual(parseApproximateYear(''), null);
    assert.deepEqual(parseApproximateYear('1986'), 1986);
    assert.deepEqual(parseApproximateYear('1899'), { error: 'Year must be between 1900 and 2100.' });
    assert.deepEqual(parseApproximateDate(''), null);
    assert.deepEqual(parseApproximateDate('1986-07-12'), '1986-07-12');
    assert.deepEqual(parseApproximateDate('1986-13-01'), { error: 'Approximate date must be a real calendar date.' });
    assert.equal(
      validatePhotoSubmit({
        title: 'Wembley',
        description: '',
        suggestedCategory: '',
        approximateYear: 'abc',
        approximateDate: '',
        photo,
      }),
      'Year must be between 1900 and 2100.',
    );
  });
});

describe('formatSubmittedAt', () => {
  it('uses the website confirmation timestamp shape', () => {
    assert.equal(formatSubmittedAt('2026-08-23T00:15:00.000Z'), '2026-08-23 00:15:00Z');
  });
});
