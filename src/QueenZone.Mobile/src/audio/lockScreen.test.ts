import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import {
  audioSessionMode,
  lockScreenAlbumTitle,
  lockScreenArtworkUrlOrOmit,
  lockScreenMetadata,
  lockScreenOptions,
} from './lockScreen.ts';

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

describe('lockScreenArtworkUrlOrOmit', () => {
  it('keeps a bundled file URI and drops network or blob URLs', () => {
    assert.equal(lockScreenArtworkUrlOrOmit('file:///app/assets/icon.png'), 'file:///app/assets/icon.png');
    assert.equal(lockScreenArtworkUrlOrOmit('asset:/icon.png'), 'asset:/icon.png');
    assert.equal(lockScreenArtworkUrlOrOmit('https://cdn.example/cover.jpg'), undefined);
    assert.equal(lockScreenArtworkUrlOrOmit('http://localhost:8081/assets/icon.png'), undefined);
    assert.equal(lockScreenArtworkUrlOrOmit('blob:https://qz.test/1'), undefined);
    assert.equal(lockScreenArtworkUrlOrOmit('  '), undefined);
    assert.equal(lockScreenArtworkUrlOrOmit(undefined), undefined);
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

  it('attaches bundled artwork and omits remote artwork', () => {
    assert.deepEqual(
      lockScreenMetadata(
        { title: 'Somebody to Love', performedBy: 'Jane' },
        'file:///app/assets/icon.png',
      ),
      {
        title: 'Somebody to Love',
        artist: 'Jane',
        albumTitle: lockScreenAlbumTitle,
        artworkUrl: 'file:///app/assets/icon.png',
      },
    );
    assert.equal(
      'artworkUrl' in
        lockScreenMetadata({ title: 'Radio Ga Ga', performedBy: 'Sam' }, 'https://cdn.example/a.jpg'),
      false,
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
