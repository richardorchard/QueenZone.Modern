import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { audioFromDocumentAsset, validateFanPerformanceSubmit } from './fanPerformanceSubmitMeta.ts';

describe('validateFanPerformanceSubmit', () => {
  const valid = {
    title: 'Reaching Out cover',
    coveredSong: 'Reaching Out',
    performedBy: 'Stage Fan',
    description: '',
    rightsDeclarationAccepted: true,
    audio: { uri: 'file://perf.mp3', name: 'perf.mp3', type: 'audio/mpeg' },
    fileSize: 1024,
  };

  it('accepts a complete pick-file submission', () => {
    assert.equal(validateFanPerformanceSubmit(valid), null);
  });

  it('requires the rights declaration', () => {
    assert.equal(
      validateFanPerformanceSubmit({ ...valid, rightsDeclarationAccepted: false }),
      'You must confirm this is your own performance and agree to publication.',
    );
  });

  it('requires an audio file', () => {
    assert.equal(validateFanPerformanceSubmit({ ...valid, audio: null }), 'Choose an audio file to upload.');
  });

  it('requires title, song, performer, and size limits', () => {
    assert.equal(validateFanPerformanceSubmit({ ...valid, title: '  ' }), 'Title is required.');
    assert.equal(
      validateFanPerformanceSubmit({ ...valid, title: 'x'.repeat(201) }),
      'Title must be 200 characters or fewer.',
    );
    assert.equal(validateFanPerformanceSubmit({ ...valid, coveredSong: '' }), 'Covered song is required.');
    assert.equal(
      validateFanPerformanceSubmit({ ...valid, coveredSong: 'x'.repeat(201) }),
      'Covered song must be 200 characters or fewer.',
    );
    assert.equal(validateFanPerformanceSubmit({ ...valid, performedBy: '' }), 'Performed by is required.');
    assert.equal(
      validateFanPerformanceSubmit({ ...valid, performedBy: 'x'.repeat(201) }),
      'Performed by must be 200 characters or fewer.',
    );
    assert.equal(
      validateFanPerformanceSubmit({ ...valid, description: 'x'.repeat(2001) }),
      'Description must be 2000 characters or fewer.',
    );
    assert.equal(
      validateFanPerformanceSubmit({ ...valid, fileSize: 26 * 1024 * 1024 }),
      'Audio must be 25 MB or smaller.',
    );
  });
});

describe('audioFromDocumentAsset', () => {
  it('defaults mp3 mime when the picker omits it', () => {
    const mapped = audioFromDocumentAsset({ uri: 'file://x.mp3', name: 'x.mp3' });
    assert.equal(mapped.file.type, 'audio/mpeg');
    assert.equal(mapped.fileSize, null);
  });

  it('maps flac files and keeps the reported size', () => {
    const mapped = audioFromDocumentAsset({
      uri: 'file://take.flac',
      name: 'take.flac',
      size: 4096,
    });
    assert.equal(mapped.file.type, 'audio/flac');
    assert.equal(mapped.fileSize, 4096);
  });

  it('names an unnamed asset performance.mp3', () => {
    const mapped = audioFromDocumentAsset({ uri: 'file://blob' });
    assert.equal(mapped.file.name, 'performance.mp3');
    assert.equal(mapped.file.type, 'audio/mpeg');
  });
});
