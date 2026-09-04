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
});

describe('audioFromDocumentAsset', () => {
  it('defaults mp3 mime when the picker omits it', () => {
    const mapped = audioFromDocumentAsset({ uri: 'file://x.mp3', name: 'x.mp3' });
    assert.equal(mapped.file.type, 'audio/mpeg');
  });
});
