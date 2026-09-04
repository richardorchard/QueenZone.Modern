import { audioFromDocumentAsset, validateFanPerformanceSubmit } from './fanPerformanceSubmitMeta';

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
    expect(validateFanPerformanceSubmit(valid)).toBeNull();
  });

  it('requires the rights declaration', () => {
    expect(validateFanPerformanceSubmit({ ...valid, rightsDeclarationAccepted: false })).toBe(
      'You must confirm this is your own performance and agree to publication.',
    );
  });

  it('requires an audio file', () => {
    expect(validateFanPerformanceSubmit({ ...valid, audio: null })).toBe('Choose an audio file to upload.');
  });
});

describe('audioFromDocumentAsset', () => {
  it('defaults mp3 mime when the picker omits it', () => {
    const mapped = audioFromDocumentAsset({ uri: 'file://x.mp3', name: 'x.mp3' });
    expect(mapped.file.type).toBe('audio/mpeg');
  });
});
