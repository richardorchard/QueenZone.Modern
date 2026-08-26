import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { audioSessionMode, lockScreenAlbumTitle, lockScreenMetadata, lockScreenOptions } from './lockScreen.ts';

describe('audioSessionMode', () => {
  it('keeps playback exclusive so lock-screen controls can bind', () => {
    assert.equal(audioSessionMode.playsInSilentMode, true);
    assert.equal(audioSessionMode.shouldPlayInBackground, true);
    assert.equal(audioSessionMode.interruptionMode, 'doNotMix');
  });
});

describe('lockScreenOptions', () => {
  it('shows seek buttons and is not a live stream', () => {
    assert.equal(lockScreenOptions.showSeekBackward, true);
    assert.equal(lockScreenOptions.showSeekForward, true);
    assert.equal('isLiveStream' in lockScreenOptions, false);
  });
});

describe('lockScreenMetadata', () => {
  it('maps title and performer only', () => {
    assert.deepEqual(
      lockScreenMetadata({
        title: 'Somebody to Love',
        performedBy: 'Jane',
      }),
      {
        title: 'Somebody to Love',
        artist: 'Jane',
        albumTitle: lockScreenAlbumTitle,
      },
    );
  });

  it('omits paths, tokens, and filenames', () => {
    const metadata = lockScreenMetadata({
      title: 'Radio Ga Ga',
      performedBy: 'Sam',
    });
    assert.deepEqual(Object.keys(metadata).sort(), ['albumTitle', 'artist', 'title']);
    assert.equal(JSON.stringify(metadata).includes('audio'), false);
    assert.equal(JSON.stringify(metadata).includes('Bearer'), false);
  });
});
