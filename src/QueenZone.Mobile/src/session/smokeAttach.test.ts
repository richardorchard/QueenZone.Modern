import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import {
  buildSmokeAttachUrl,
  defaultSmokeAttachAsset,
  isSmokeAttachEnabled,
  parseSmokeAttachAsset,
  peekPendingSmokeAttachAsset,
  resetSmokeAttachPending,
  smokeAttachDefaultAndroidUri,
  smokeAttachFileName,
  smokeAttachHost,
  stashSmokeAttachAsset,
  takePendingSmokeAttachAsset,
} from './smokeAttach.ts';
import { smokeAuthScheme } from './smokeAuth.ts';

describe('smokeAttach', () => {
  it('uses the same Debug/development gate as smoke-auth', () => {
    assert.equal(isSmokeAttachEnabled({ dev: true, appEnv: 'development' }), true);
    assert.equal(isSmokeAttachEnabled({ dev: true, appEnv: 'staging' }), false);
    assert.equal(isSmokeAttachEnabled({ dev: false, appEnv: 'development' }), false);
    assert.equal(isSmokeAttachEnabled({}), false);
  });

  it('parses a smoke-attach deep link and ignores smoke-auth', () => {
    const url = buildSmokeAttachUrl('file:///sdcard/Download/attach.txt', {
      name: smokeAttachFileName,
      type: 'text/plain',
    });
    assert.equal(url.startsWith(`${smokeAuthScheme}://${smokeAttachHost}?`), true);
    assert.deepEqual(parseSmokeAttachAsset(url), {
      uri: 'file:///sdcard/Download/attach.txt',
      name: smokeAttachFileName,
      mimeType: 'text/plain',
    });
    assert.equal(parseSmokeAttachAsset('queenzone://smoke-auth?accessToken=tok'), null);
    assert.equal(parseSmokeAttachAsset('queenzone://smoke-attach'), null);
    assert.equal(parseSmokeAttachAsset('not-a-url'), null);
  });

  it('rejects an empty URI when building a smoke-attach URL', () => {
    assert.throws(() => buildSmokeAttachUrl('   '), /non-empty file URI/);
  });

  it('defaults Android to the app-private files URI', () => {
    assert.deepEqual(defaultSmokeAttachAsset('android'), {
      uri: smokeAttachDefaultAndroidUri,
      name: smokeAttachFileName,
      mimeType: 'text/plain',
    });
    assert.equal(defaultSmokeAttachAsset('ios').name, smokeAttachFileName);
  });

  it('stashes and consumes one pending inject', () => {
    resetSmokeAttachPending();
    stashSmokeAttachAsset({
      uri: 'file:///tmp/attach.txt',
      name: smokeAttachFileName,
      mimeType: 'text/plain',
    });
    assert.equal(peekPendingSmokeAttachAsset()?.uri, 'file:///tmp/attach.txt');
    assert.equal(takePendingSmokeAttachAsset()?.name, smokeAttachFileName);
    assert.equal(takePendingSmokeAttachAsset(), null);
  });
});
