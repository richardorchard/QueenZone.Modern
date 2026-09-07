import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import {
  fileLooksLikeHttpError,
  looksLikeHttpErrorPayload,
  resolveDownloadAudioExtension,
} from './audioBytes.ts';

describe('resolveDownloadAudioExtension', () => {
  it('identifies MP3 and FLAC bytes and falls back to the response type', () => {
    assert.equal(resolveDownloadAudioExtension(new Uint8Array([0x49, 0x44, 0x33, 0x04])), 'mp3');
    assert.equal(resolveDownloadAudioExtension(new Uint8Array([0xff, 0xfb, 0x90, 0x00])), 'mp3');
    assert.equal(resolveDownloadAudioExtension(new Uint8Array([0x66, 0x4c, 0x61, 0x43])), 'flac');
    assert.equal(resolveDownloadAudioExtension(null, 'audio/flac; charset=binary'), 'flac');
    assert.equal(resolveDownloadAudioExtension(null, 'audio/mpeg'), 'mp3');
    assert.equal(resolveDownloadAudioExtension(null, null), 'mp3');
  });

  it('prefers the file signature over response metadata', () => {
    assert.equal(
      resolveDownloadAudioExtension(new Uint8Array([0x66, 0x4c, 0x61, 0x43]), 'audio/mpeg'),
      'flac',
    );
  });
});

describe('looksLikeHttpErrorPayload', () => {
  it('treats JSON and HTML prefixes as error bodies', () => {
    assert.equal(looksLikeHttpErrorPayload(new Uint8Array([0x7b, 0x22])), true);
    assert.equal(looksLikeHttpErrorPayload(new Uint8Array([0x20, 0x3c, 0x68])), true);
    assert.equal(looksLikeHttpErrorPayload(new Uint8Array([0x20, 0x20])), false);
    assert.equal(looksLikeHttpErrorPayload(new Uint8Array([1, 2, 3, 4])), false);
    assert.equal(looksLikeHttpErrorPayload(new Uint8Array([0x49, 0x44, 0x33])), false);
  });
});

describe('fileLooksLikeHttpError', () => {
  it('rejects empty files and sniffs only small payloads', async () => {
    assert.equal(
      await fileLooksLikeHttpError(async () => new Uint8Array([0x7b]), 'file:///x', 0),
      true,
    );
    assert.equal(
      await fileLooksLikeHttpError(async () => new Uint8Array([0x7b]), 'file:///x', 200),
      true,
    );
    assert.equal(
      await fileLooksLikeHttpError(async () => new Uint8Array([0x7b]), 'file:///x', 9000),
      false,
    );
  });
});
